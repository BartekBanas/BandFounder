import {act, renderHook, waitFor} from '@testing-library/react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import {getGenres, getMusicianRoles} from '../../../api/metadata';
import {SlotType} from '../../../types/MusicianSlot';
import {useListingEditorState} from './useListingEditorState';

vi.mock('../../../api/metadata', () => ({
    getGenres: vi.fn(),
    getMusicianRoles: vi.fn(),
}));

const initialValues = {
    name: 'Synth Collective',
    type: 'Band',
    genre: 'Electronic',
    description: 'Looking for collaborators',
    musicianSlots: [{id: 'slot-1', role: 'Drummer', status: SlotType.Available}],
};

describe('useListingEditorState', () => {
    beforeEach(() => {
        vi.mocked(getGenres).mockResolvedValue(['Electronic', 'Jazz']);
        vi.mocked(getMusicianRoles).mockResolvedValue(['Drummer', 'Bassist']);
    });

    it('adds, edits, and deletes musician slots', async () => {
        const {result} = renderHook(() => useListingEditorState(initialValues));

        await waitFor(() => expect(result.current.genres).toEqual(['Electronic', 'Jazz']));
        expect(result.current.roles).toEqual(['Drummer', 'Bassist']);

        act(() => result.current.addSlot());

        const addedSlot = result.current.listingMusicianSlots[1];
        expect(addedSlot).toMatchObject({role: '', status: SlotType.Available});
        expect(addedSlot.id).toBeTruthy();

        act(() => {
            result.current.editSlotRole(addedSlot.id!, 'Bassist');
            result.current.editSlotStatus(addedSlot.id!, 'Filled');
        });

        expect(result.current.listingMusicianSlots[1]).toEqual({
            id: addedSlot.id,
            role: 'Bassist',
            status: SlotType.Filled,
        });

        act(() => result.current.editSlotStatus(addedSlot.id!, 'Unknown'));

        expect(result.current.listingMusicianSlots[1].status).toBe(SlotType.Filled);

        act(() => result.current.deleteSlot(addedSlot.id!));

        expect(result.current.listingMusicianSlots).toEqual(initialValues.musicianSlots);
    });

    it('serializes create and update payloads without changing API contracts', () => {
        const {result} = renderHook(() => useListingEditorState(initialValues));

        act(() => {
            result.current.setListingName('New Project');
            result.current.editSlotStatus('slot-1', 'Filled');
        });

        expect(result.current.toCreatePayload()).toEqual({
            name: 'New Project',
            type: 'Band',
            genre: 'Electronic',
            description: 'Looking for collaborators',
            musicianSlots: [{role: 'Drummer', status: SlotType.Filled}],
        });
        expect(result.current.toUpdatePayload()).toEqual({
            name: 'New Project',
            type: 'Band',
            genre: 'Electronic',
            description: 'Looking for collaborators',
            musicianSlots: [{id: 'slot-1', role: 'Drummer', status: SlotType.Filled}],
        });
    });

    it('resets editor state when reset is called', () => {
        const {result} = renderHook(() => useListingEditorState(initialValues));

        act(() => {
            result.current.setListingName('Draft title');
            result.current.setListingDescription('Draft description');
        });

        expect(result.current.listingName).toBe('Draft title');
        expect(result.current.listingDescription).toBe('Draft description');

        act(() => {
            result.current.reset({
                ...initialValues,
                name: 'Updated title',
                description: '',
            });
        });

        expect(result.current.listingName).toBe('Updated title');
        expect(result.current.listingDescription).toBe('');
        expect(result.current.toUpdatePayload().description).toBe('');
    });
});
