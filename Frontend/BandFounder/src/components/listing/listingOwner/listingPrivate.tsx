import React, {useEffect, useState} from 'react';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {Button} from "@mui/material";
import EditIcon from '@mui/icons-material/Edit';
import {getListing, updateListing} from "../../../api/listing";
import {getGenres, getMusicianRoles} from "../../../api/metadata";
import ProfilePicture from "../../profile/ProfilePicture";
import {DeleteListingButton} from "./DeleteListingButton";
import {MusicianSlot, SlotType} from "../../../types/MusicianSlot";
import {Listing} from "../../../types/Listing";
import {ListingUpdate} from "../../../types/ListingUpdate";
import ListingCard from "../shared/ListingCard";
import ListingCardHeader from "../shared/ListingCardHeader";
import ListingCardBody from "../shared/ListingCardBody";
import AvailableRolesSection from "../shared/AvailableRolesSection";
import ListingEditorModal from "../shared/ListingEditorModal";

interface ListingPrivateProps {
    listingId: string;
}

const ListingPrivate: React.FC<ListingPrivateProps> = ({listingId}) => {
    const [listing, setListing] = useState<Listing>();
    const [open, setOpen] = useState(false);
    const [listingType, setListingType] = useState<string>('');
    const [listingGenre, setListingGenre] = useState<string>('');
    const [listingDescription, setListingDescription] = useState<string>('');
    const [listingMusicianSlots, setListingMusicianSlots] = useState<MusicianSlot[]>([]);
    const [listingName, setListingName] = useState<string>('');
    const [genres, setGenres] = useState<string[]>([]);
    const [roles, setRoles] = useState<string[]>([]);

    useEffect(() => {
        const fetchListing = async () => {
            const data = await getListing(listingId);
            if (data) {
                setListing(data);
                setListingType(data.type);
                setListingGenre(data.genre);
                setListingDescription(data.description);
                setListingMusicianSlots(data.musicianSlots);
                setListingName(data.name);
            }
        };

        const fetchGenres = async () => {
            const genres = await getGenres();
            if (genres) {
                setGenres(genres);
            }
        }

        const fetchRoles = async () => {
            const roles = await getMusicianRoles();
            if (roles) {
                setRoles(roles);
            }
        }

        fetchListing();
        fetchGenres();
        fetchRoles();
    }, [listingId]);

    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const handleEditSlotStatus = (slotId: string, status: string) => {
        setListingMusicianSlots((slots) =>
            slots.map((slot) => slot.id === slotId ? {...slot, status: status as SlotType} : slot)
        );
    };

    const handleEditMusicianRole = (slotId: string, role: string) => {
        setListingMusicianSlots((slots) =>
            slots.map((slot) => slot.id === slotId ? {...slot, role} : slot)
        );
    };

    const handleAddNewRole = () => {
        const newSlot: MusicianSlot = {
            id: Math.random().toString(36).substr(2, 9),
            role: '',
            status: SlotType.Available,
        };
        setListingMusicianSlots((slots) => [...slots, newSlot]);
    };

    const handleUpdateListing = async () => {
        try {
            const updatedListing: ListingUpdate = {
                name: listingName,
                type: listingType,
                genre: listingGenre,
                description: listingDescription,
                musicianSlots: listingMusicianSlots,
            }
            await updateListing(updatedListing, listingId);
            window.location.reload();
        } catch (e) {
            console.error(e);
        }
    };

    const handleDeleteRole = (slotId: string) => {
        setListingMusicianSlots((slots) => slots.filter((slot) => slot.id !== slotId));
    };

    if (!listing) {
        const theme = createTheme({
            components: {
                Loader: Loader.extend({
                    defaultProps: {
                        loaders: {...Loader.defaultLoaders, ring: RingLoader},
                        type: 'ring',
                    },
                }),
            },
        });
        return (
            <div className="App-header">
                <MantineThemeProvider theme={theme}>
                    <Loader size={200}/>
                </MantineThemeProvider>
            </div>
        );
    }

    return (
        <ListingCard
            className="custom-scrollbar"
            actions={
                <>
                    <Button variant="contained" color="info" size="small" onClick={handleOpen}>
                        <span>Edit</span> <EditIcon/>
                    </Button>
                    <DeleteListingButton listingId={listingId}/>
                </>
            }
        >
            <ListingCardHeader
                ownerElement={
                    <div className="owner-listing-elements">
                        <ProfilePicture isMyProfile={true} accountId={listing.ownerId} size={40}/>
                        <p>{listing?.owner?.name}</p>
                    </div>
                }
                title={listing.name}
                type={listing.type}
                genre={listing.genre}
            />
            <ListingCardBody description={listing.description}/>
            <AvailableRolesSection slots={listing.musicianSlots}/>

            <ListingEditorModal
                open={open}
                title="Edit Listing"
                submitLabel="Save"
                onClose={handleClose}
                onSubmit={handleUpdateListing}
                listingName={listingName}
                onListingNameChange={setListingName}
                listingType={listingType}
                onListingTypeChange={setListingType}
                listingGenre={listingGenre}
                onListingGenreChange={setListingGenre}
                listingDescription={listingDescription}
                onListingDescriptionChange={setListingDescription}
                musicianSlots={listingMusicianSlots}
                onAddRole={handleAddNewRole}
                onDeleteRole={handleDeleteRole}
                onEditMusicianRole={handleEditMusicianRole}
                onEditSlotStatus={handleEditSlotStatus}
                genres={genres}
                roles={roles}
            />
        </ListingCard>
    );
};

export default ListingPrivate;
