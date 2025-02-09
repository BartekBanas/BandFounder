using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Exceptions;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using FluentValidation;

namespace BandFounder.Application.Services;

public interface IListingService
{
    Task<Listing?> GetListingAsync(Guid listingId);
    Task<IEnumerable<Listing>> GetListingsAsync();
    Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions);
    Task<IEnumerable<Listing>> GetUserListingsAsync(Guid? accountId = null);
    Task<ArtistsAndGenresDto> GetCommonArtistsAndGenresWithListingsAsync(Guid listingId, Guid? accountId = null);
    Task<Listing> CreateListingAsync(ListingCreateDto dto, Guid? accountId = null);
    Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? listingId = null);
    Task<ChatroomDto> ContactOwner(Guid listingId);
    Task DeleteListing(Guid listingId);
    Task UpdateListing(Guid listingId, ListingCreateDto dto);
}

public class ListingService : IListingService
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IMusicTasteService _musicTasteService;
    private readonly IChatroomService _chatroomService;
    
    private readonly IValidator<Listing> _listingValidator;
    
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<MusicianSlot> _musicianSlotRepository;
    private readonly IRepository<Listing> _listingRepository;
    
    private Guid CurrentUserId => _authenticationService.GetUserId();

    public ListingService(
        IAccountService accountService,
        IAuthenticationService authenticationService,
        IMusicTasteService musicTasteService,
        IChatroomService chatroomService,
        IValidator<Listing> listingValidator,
        IRepository<Genre> genreRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<MusicianSlot> musicianSlotRepository,
        IRepository<Listing> listingRepository)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _musicTasteService = musicTasteService;
        _chatroomService = chatroomService;
        _listingValidator = listingValidator;
        _genreRepository = genreRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _musicianSlotRepository = musicianSlotRepository;
        _listingRepository = listingRepository;
    }

    public async Task<Listing?> GetListingAsync(Guid listingId)
    {
        var listing = await _listingRepository.GetOneAsync(
            filter: listing => listing.Id == listingId,
            includeProperties:
            [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);
        
        return listing;
    }
    
    public async Task<IEnumerable<Listing>> GetListingsAsync()
    {
        var listings = await _listingRepository.GetAsync(includeProperties:
            [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);

        return listings;
    }
    
    public async Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions)
    {
        var userId = _authenticationService.GetUserId();
        var userAccount = await _accountService.GetDetailedAccount(userId);
        
        var listings = await _listingRepository.GetAsync(includeProperties:
            [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);

        var listingsList = listings.ToList();
        FilterListings(userAccount, listingsList, filterOptions);
        
        var listingsWithScores = new List<ListingWithScore>();

        foreach (var listing in listingsList)
        {
            var similarityScore = await _musicTasteService.CompareMusicTasteAsync(userId, listing.OwnerId);
            listingsWithScores.Add(new ListingWithScore()
            {
                Listing = listing.ToDto(),
                SimilarityScore = similarityScore
            });
        }
        
        listingsWithScores = listingsWithScores
            .OrderByDescending(listing => listing.SimilarityScore)
            .ToList();
        
        if (filterOptions is { PageNumber: not null, PageSize: not null })
        {
            listingsWithScores = listingsWithScores
                .Skip((filterOptions.PageNumber.Value - 1) * filterOptions.PageSize.Value)
                .Take(filterOptions.PageSize.Value)
                .ToList();
        }
        
        return new ListingsFeedDto
        {
            Listings = listingsWithScores
        };
    }
    
    public async Task<IEnumerable<Listing>> GetUserListingsAsync(Guid? accountId = null)
    {
        accountId ??= CurrentUserId;
        
        var myListings = await _listingRepository.GetAsync(
            filter: listing => listing.OwnerId == accountId,
            includeProperties: [nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role"]);

        return myListings;
    }

    public async Task<ArtistsAndGenresDto> GetCommonArtistsAndGenresWithListingsAsync(Guid listingId, Guid? accountId = null)
    {
        var userId = accountId ?? CurrentUserId;
        
        Listing listing;
        try
        {
            listing = await _listingRepository.GetOneRequiredAsync(listingId);
        }
        catch (Exception e)
        {
            throw new NotFoundException("Could not find listing");
        }
        
        var commonArtists = await _musicTasteService.GetCommonArtists(userId, listing.OwnerId);
        var commonGenres = await _musicTasteService.GetCommonGenres(userId, listing.OwnerId);
        
        return new ArtistsAndGenresDto(commonArtists, commonGenres);
    }

    public async Task<Listing> CreateListingAsync(ListingCreateDto dto, Guid? accountId = null)
    {
        var userId = accountId ?? CurrentUserId;
        await _accountService.GetAccountAsync(userId);

        var projectGenre = string.IsNullOrWhiteSpace(dto.Genre) ? null : await _genreRepository.GetOrCreateAsync(dto.Genre);

        var listing = new Listing
        {
            OwnerId = userId,
            Name = dto.Name,
            Genre = projectGenre,
            Type = dto.Type,
            Description = dto.Description ?? ""
        };

        foreach (var slotDto in dto.MusicianSlots)
        {
            MusicianRole role;
            try
            {
                role = await _musicianRoleRepository.GetOrCreateAsync(slotDto.Role);
            }
            catch (Exception e)
            {
                throw new BadRequestException(e.Message);
            }

            var musicianSlot = new MusicianSlot
            {
                Role = role,
                Status = slotDto.Status,
                Listing = listing
            };

            listing.MusicianSlots.Add(musicianSlot);
        }
        
        var validationResult = await _listingValidator.ValidateAsync(listing);
        if (validationResult.IsValid is false)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await _listingRepository.CreateAsync(listing);
        await _musicianRoleRepository.SaveChangesAsync();

        return listing;
    }

    public async Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? listingId = null)
    {
        var musicianSlot = await _musicianSlotRepository.GetOneRequiredAsync(slotId);
        var listing = await _listingRepository.GetOneRequiredAsync
            (listing => listing.Id == musicianSlot.ListingId);

        if (CurrentUserId != listing.OwnerId)
        {
            throw new ForbiddenException("You do not have access to this music project listing");
        }
        
        musicianSlot.Status = slotStatus;
        
        await _musicianSlotRepository.SaveChangesAsync();
    }

    public async Task<ChatroomDto> ContactOwner(Guid listingId)
    {
        var issuer = await _accountService.GetAccountAsync(_authenticationService.GetUserId());
        var listing = await _listingRepository.GetOneRequiredAsync(listingId);
        
        var chatroomCreateDto = new ChatroomCreateDto()
        {
            ChatRoomType = ChatRoomType.Direct,
            InvitedAccountId = listing.OwnerId
        };

        return await _chatroomService.CreateChatroom(issuer, chatroomCreateDto);
    }

    public async Task DeleteListing(Guid listingId)
    {
        var listing = await _listingRepository.GetOneRequiredAsync(listingId);

        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("You may only delete your own listings.");
        }
        
        await _listingRepository.DeleteOneAsync(listing.Id);
        await _listingRepository.SaveChangesAsync();
    }

    public async Task UpdateListing(Guid listingId, ListingUpdateDto dto)
    {
        var listing = await GetListingAsync(listingId);
        if (listing is null)
        {
            throw new NotFoundException("Listing not found");
        }
        
        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("You cannot edit this listing");
        }

        // Update basic listing details
        listing.Name = dto.Name;
        listing.Genre = string.IsNullOrWhiteSpace(dto.Genre) ? null : await _genreRepository.GetOrCreateAsync(dto.Genre);
        listing.GenreName = dto.Genre;
        listing.Type = dto.Type;
        listing.Description = dto.Description ?? "";

        // Process MusicianSlots
        var existingSlots = listing.MusicianSlots.ToList();
        var updatedSlots = new List<MusicianSlot>();

        foreach (var slotDto in dto.MusicianSlots)
        {
            var role = await _musicianRoleRepository.GetOrCreateAsync(slotDto.Role);
            var existingSlot = existingSlots.FirstOrDefault(musicianSlot => musicianSlot.Role.Name == role.Name);

            if (existingSlot != null)
            {
                // Update existing slot if it exists
                existingSlot.Status = slotDto.Status;
                updatedSlots.Add(existingSlot);
                existingSlots.Remove(existingSlot); // Remove from the list of slots to delete
            }
            else
            {
                // Add new slot if no matching slot exists
                updatedSlots.Add(new MusicianSlot
                {
                    Role = role,
                    Status = slotDto.Status,
                    ListingId = listingId
                });
            }
        }

        // Remove slots that were not updated (i.e. slots that are no longer part of the new dto)
        foreach (var slotToRemove in existingSlots)
        {
            await _musicianSlotRepository.DeleteOneAsync(slotToRemove.Id);
        }

        // Assign updated slots to the listing
        listing.MusicianSlots = updatedSlots;
        
        var validationResult = await _listingValidator.ValidateAsync(listing);
        if (validationResult.IsValid is false)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Save the changes
        await _listingRepository.UpdateAsync(listing, listing.Id);
        await _listingRepository.SaveChangesAsync();
    }

    private void FilterListings(Account account, List<Listing> listings, FeedFilterOptions filterOptions)
    {
        if (filterOptions.ExcludeOwn)
        {
            listings.RemoveAll(listing => listing.OwnerId == account.Id);
        }
        
        if (filterOptions.MatchRole && account.MusicianRoles.Count > 0)
        {
            listings.RemoveAll(listing => 
                !listing.MusicianSlots.Any(slot => 
                    slot.Status == SlotStatus.Available &&
                    account.MusicianRoles.Any(role => role.Name == "Any" || role.Name == slot.Role.Name)
                )
            );
        }

        if (filterOptions.ListingType is not null)
        {
            listings.RemoveAll(listing => listing.Type != filterOptions.ListingType);
        }

        if (filterOptions.Genre is not null)
        {
            listings.RemoveAll(listing => listing.GenreName != filterOptions.Genre);
        }
        
        if (filterOptions.FromLatest)
        {
            listings.Sort((x, y) => y.DateCreated.CompareTo(x.DateCreated));
        }
    }
}