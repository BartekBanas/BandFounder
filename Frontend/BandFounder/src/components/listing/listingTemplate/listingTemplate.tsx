import React, {useEffect, useState} from 'react';
import {ListingCreate} from "../../../types/ListingCreate";
import {getUser} from "../../../api/account";
import {postListing} from "../../../api/listing";
import {getGenres, getMusicianRoles} from "../../../api/metadata";
import {getUserId} from "../../../hooks/authentication";
import CreateListingCard from "../shared/CreateListingCard";
import ListingEditorModal, {EditorMusicianSlot} from "../shared/ListingEditorModal";

const ListingTemplate: React.FC = () => {
    const [user, setUser] = useState<any>(null);
    const [modalOpen, setModalOpen] = useState<boolean>(false);
    const [listingType, setListingType] = useState<string>('CollaborativeSong');
    const [listingGenre, setListingGenre] = useState<string>('');
    const [listingDescription, setListingDescription] = useState<string>('');
    const [listingMusicianSlots, setListingMusicianSlots] = useState<EditorMusicianSlot[]>([]);
    const [listingName, setListingName] = useState<string>('');
    const [genres, setGenres] = useState<string[]>([]);
    const [roles, setRoles] = useState<string[]>([]);

    useEffect(() => {
        const fetchData = async () => {
            const userId = getUserId();
            const userData = await getUser(userId);
            setUser(userData);
        };

        fetchData();
    }, []);

    useEffect(() => {
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

        fetchGenres();
        fetchRoles();
    }, []);

    if (!user) {
        return null;
    }

    const handleListingClick = () => {
        setModalOpen(true);
    };

    const handleEditSlotStatus = (slotId: string, status: string) => {
        setListingMusicianSlots((slots) =>
            slots.map((slot) => slot.id === slotId ? {...slot, status} : slot)
        );
    };

    const handleEditMusicianRole = (slotId: string, role: string) => {
        setListingMusicianSlots((slots) =>
            slots.map((slot) => slot.id === slotId ? {...slot, role} : slot)
        );
    };

    const handleAddNewRole = () => {
        const newSlot: EditorMusicianSlot = {
            id: Math.random().toString(36).substr(2, 9),
            role: '',
            status: 'Available',
        };
        setListingMusicianSlots((slots) => [...slots, newSlot]);
    };

    const handleDeleteRole = (slotId: string) => {
        setListingMusicianSlots((slots) => slots.filter((slot) => slot.id !== slotId));
    };

    const handlePostListing = async () => {
        try {
            const createdListing: ListingCreate = {
                name: listingName,
                type: listingType,
                genre: listingGenre,
                description: listingDescription,
                musicianSlots: listingMusicianSlots,
            }
            await postListing(createdListing);
            window.location.reload();
        } catch (e) {
            console.log(e);
        }
    };

    return (
        <>
            <CreateListingCard onClick={handleListingClick}/>
            <ListingEditorModal
                open={modalOpen}
                title="Create Listing"
                submitLabel="Post"
                onClose={() => setModalOpen(false)}
                onSubmit={handlePostListing}
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
        </>
    );
};

export default ListingTemplate;
