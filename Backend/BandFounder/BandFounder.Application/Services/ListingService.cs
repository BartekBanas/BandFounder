using System.Linq.Expressions;
using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services;

public interface IListingService
{
    Task<Listing?> GetListingAsync(Guid listingId);
    Task<IEnumerable<Listing>> GetListingsAsync();
    Task<ListingsFeedDto> GetListingsFeedAsync(FeedFilterOptions filterOptions);
    Task<IEnumerable<Listing>> GetUserListingsAsync(Guid? accountId = null);
    Task<Listing> CreateListingAsync(ListingCreateDto dto, Guid? accountId = null);
    Task UpdateSlotStatus(Guid slotId, SlotStatus slotStatus, Guid? listingId = null);
    Task AssignUserToSlot(Guid slotId, Guid accountId);
    Task<ChatroomDto> ContactOwner(Guid listingId);
    Task DeleteListing(Guid listingId);
    Task UpdateListing(Guid listingId, ListingUpdateDto dto);
}

public class ListingService : IListingService
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
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
        IAuthorizationService authorizationService,
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
        _authorizationService = authorizationService;
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
        // Build a database-side filter to reduce the number of loaded listings
        var userId = _authenticationService.GetUserId();
        var userAccount = await _accountService.GetDetailedAccount(userId);

        // Start with a predicate that's always true and add conditions
        Expression<Func<Listing, bool>> filter = listing => true;

        if (filterOptions.ExcludeOwn)
        {
            filter = AndAlso(filter, listing => listing.OwnerId != userId);
        }

        if (filterOptions.ListingType is not null)
        {
            var type = filterOptions.ListingType.Value;
            filter = AndAlso(filter, listing => listing.Type == type);
        }

        if (filterOptions.Genre is not null)
        {
            var genre = filterOptions.Genre;
            filter = AndAlso(filter, listing => listing.GenreName == genre);
        }

        if (filterOptions.MatchRole && userAccount.MusicianRoles.Count > 0)
        {
            var roleNames = userAccount.MusicianRoles.Select(r => r.Name).ToList();
            if (roleNames.Contains("Any"))
            {
                filter = AndAlso(filter, listing => listing.MusicianSlots.Any(slot => slot.Status == SlotStatus.Available));
            }
            else
            {
                // Use local list contains (translates to SQL IN) to match slot role names
                filter = AndAlso(filter, listing => listing.MusicianSlots.Any(slot => slot.Status == SlotStatus.Available && roleNames.Contains(slot.Role.Name)));
            }
        }

        // Fetch all listings matching the filter. Paging is applied after similarity scoring below,
        // because the feed is ranked by music-taste match rather than date or insertion order.
        var includeProperties = new[] { nameof(Listing.Owner), nameof(Listing.MusicianSlots), "MusicianSlots.Role" };

        Func<IQueryable<Listing>, IOrderedQueryable<Listing>>? orderBy = null;
        if (filterOptions.FromLatest)
        {
            orderBy = q => q.OrderByDescending(l => l.DateCreated);
        }

        var listingsList = (await _listingRepository.GetAsync(
            filter: filter,
            orderBy: orderBy,
            includeProperties: includeProperties)).ToList();

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

        var userClaims = _authenticationService.GetUserClaims();
        await _authorizationService.AuthorizeRequiredAsync(userClaims, listing, AuthorizationPolicies.IsOwnerOf);
        
        musicianSlot.Status = slotStatus;
        
        await _musicianSlotRepository.SaveChangesAsync();
    }

    public async Task AssignUserToSlot(Guid slotId, Guid accountId)
    {
        var musicianSlot = await _musicianSlotRepository.GetOneRequiredAsync(slotId);
        var listing = await _listingRepository.GetOneRequiredAsync
            (listing => listing.Id == musicianSlot.ListingId);
        
        var userClaims = _authenticationService.GetUserClaims();
        await _authorizationService.AuthorizeRequiredAsync(userClaims, listing, AuthorizationPolicies.IsOwnerOf);
        
        var invitedAccount = await _accountService.GetAccountAsync(accountId);
        if (invitedAccount == null)
        {
            throw new NotFoundException("Assignee account not found");
        }
        
        musicianSlot.AssigneeId = invitedAccount.Id;
        
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
        
        var userClaims = _authenticationService.GetUserClaims();
        await _authorizationService.AuthorizeRequiredAsync(userClaims, listing, AuthorizationPolicies.IsOwnerOf);
        
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
        
        var userClaims = _authenticationService.GetUserClaims();
        await _authorizationService.AuthorizeRequiredAsync(userClaims, listing, AuthorizationPolicies.IsOwnerOf);
        
        if (HasDuplicateSlots(dto))
        {
            throw new BadRequestException("Slots' IDs must be unique");
        }

        // Update basic listing details
        listing.Name = dto.Name;
        listing.Genre = string.IsNullOrWhiteSpace(dto.Genre) ? null : await _genreRepository.GetOrCreateAsync(dto.Genre);
        listing.GenreName = dto.Genre;
        listing.Type = dto.Type;
        if (dto.Description is not null)
        {
            listing.Description = dto.Description;
        }

        for (int i = listing.MusicianSlots.Count - 1; i >= 0; i--)
        {
            var existingSlot = listing.MusicianSlots[i];
            var matchedSlotDto = dto.MusicianSlots.FirstOrDefault(slotUpdateDto => slotUpdateDto.Id == existingSlot.Id);

            if (matchedSlotDto != null)
            {
                existingSlot.Status = matchedSlotDto.Status;
                existingSlot.Role = await _musicianRoleRepository.GetOrCreateAsync(matchedSlotDto.Role);
            }
            else
            {
                listing.MusicianSlots.RemoveAt(i);
            }
        }

        foreach (var slotDto in dto.MusicianSlots.Where(slotUpdateDto => slotUpdateDto.Id == null))
        {
            listing.MusicianSlots.Add(new MusicianSlot
            {
                Role = await _musicianRoleRepository.GetOrCreateAsync(slotDto.Role),
                Status = slotDto.Status,
                ListingId = listingId
            });
        }

        var validationResult = await _listingValidator.ValidateAsync(listing);
        if (validationResult.IsValid is false)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await _listingRepository.SaveChangesAsync();
    }

    private bool HasDuplicateSlots(ListingUpdateDto listingUpdateDto)
    {
        var duplicateSlot = listingUpdateDto.MusicianSlots
            .GroupBy(slot => slot.Id)
            .FirstOrDefault(group => group.Count() > 1);
        
        return duplicateSlot != null;
    }

    // Helper to combine expressions
    private static Expression<Func<T, bool>> AndAlso<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ParameterRebinder { Map = { [left.Parameters[0]] = parameter } };
        var leftBody = leftVisitor.Visit(left.Body);

        var rightVisitor = new ParameterRebinder { Map = { [right.Parameters[0]] = parameter } };
        var rightBody = rightVisitor.Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private class ParameterRebinder : ExpressionVisitor
    {
        public readonly Dictionary<ParameterExpression, ParameterExpression> Map = new();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Map.GetValueOrDefault(node, node);
        }
    }
}