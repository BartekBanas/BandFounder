import {useCallback, useEffect, useState} from 'react';
import {getGenres, getMusicianRoles} from '../../../api/metadata';
import {ListingCreate} from '../../../types/ListingCreate';
import {ListingUpdate} from '../../../types/ListingUpdate';
import {MusicianSlot, SlotType, castToSlotType} from '../../../types/MusicianSlot';

export interface ListingEditorInitialValues {
    name: string;
    type: string;
    genre: string;
    description: string;
    musicianSlots: MusicianSlot[];
}

const createSlotId = () => Math.random().toString(36).slice(2, 11);

export function useListingEditorState(initialValues: ListingEditorInitialValues) {
    const [listingName, setListingName] = useState(initialValues.name);
    const [listingType, setListingType] = useState(initialValues.type);
    const [listingGenre, setListingGenre] = useState(initialValues.genre);
    const [listingDescription, setListingDescription] = useState(initialValues.description ?? '');
    const [listingMusicianSlots, setListingMusicianSlots] = useState<MusicianSlot[]>(initialValues.musicianSlots);
    const [genres, setGenres] = useState<string[]>([]);
    const [roles, setRoles] = useState<string[]>([]);

    useEffect(() => {
        void getGenres().then((loadedGenres) => setGenres(loadedGenres ?? []));
        void getMusicianRoles().then((loadedRoles) => setRoles(loadedRoles ?? []));
    }, []);

    const reset = useCallback((values: ListingEditorInitialValues) => {
        setListingName(values.name);
        setListingType(values.type);
        setListingGenre(values.genre);
        setListingDescription(values.description ?? '');
        setListingMusicianSlots(values.musicianSlots);
    }, []);

    const addSlot = () => {
        setListingMusicianSlots((slots) => [
            ...slots,
            {id: createSlotId(), role: '', status: SlotType.Available},
        ]);
    };

    const deleteSlot = (slotId: string) => {
        setListingMusicianSlots((slots) => slots.filter((slot) => slot.id !== slotId));
    };

    const editSlotRole = (slotId: string, role: string) => {
        setListingMusicianSlots((slots) =>
            slots.map((slot) => slot.id === slotId ? {...slot, role} : slot),
        );
    };

    const editSlotStatus = (slotId: string, status: string) => {
        const slotStatus = castToSlotType(status);
        if (!slotStatus) {
            return;
        }

        setListingMusicianSlots((slots) =>
            slots.map((slot) => slot.id === slotId ? {...slot, status: slotStatus} : slot),
        );
    };

    const toCreatePayload = (): ListingCreate => ({
        name: listingName,
        type: listingType,
        genre: listingGenre,
        description: listingDescription ?? '',
        musicianSlots: listingMusicianSlots.map(({role, status}) => ({role, status})),
    });

    const toUpdatePayload = (): ListingUpdate => ({
        name: listingName,
        type: listingType,
        genre: listingGenre,
        description: listingDescription,
        musicianSlots: listingMusicianSlots,
    });

    return {
        listingName,
        setListingName,
        listingType,
        setListingType,
        listingGenre,
        setListingGenre,
        listingDescription,
        setListingDescription,
        listingMusicianSlots,
        genres,
        roles,
        addSlot,
        deleteSlot,
        editSlotRole,
        editSlotStatus,
        toCreatePayload,
        toUpdatePayload,
        reset,
    };
}
