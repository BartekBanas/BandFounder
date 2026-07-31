import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import {getMyAccount, updateMyAccount} from '../../api/account';
import {getTopArtists} from '../../api/spotify';
import {getUsersGenres} from '../../api/metadata';
import {mantineErrorNotification} from '../common/mantineNotification';
import {UtilityDrawer} from './UtilityDrawer';

vi.mock('../../api/account', () => ({
    getMyAccount: vi.fn(),
    updateMyAccount: vi.fn(),
}));

vi.mock('../../api/spotify', () => ({
    getTopArtists: vi.fn(),
}));

vi.mock('../../api/metadata', () => ({
    getUsersGenres: vi.fn(),
}));

vi.mock('../../hooks/authentication', () => ({
    getUserId: vi.fn(() => 'account-1'),
    removeAuthToken: vi.fn(),
    removeUserId: vi.fn(),
}));

vi.mock('../common/mantineNotification', () => ({
    mantineErrorNotification: vi.fn(),
    mantineSuccessNotification: vi.fn(),
}));

vi.mock('./DeleteAccountButton', () => ({
    DeleteAccountButton: () => <button>Delete Account</button>,
}));

vi.mock('./UpdateAccountButton', () => ({
    UpdateAccountButton: () => <button>Update Account</button>,
}));

vi.mock('./spotifyConnection/SpotifyConnectionButton', () => ({
    SpotifyConnectionButton: () => <button>Connect Spotify</button>,
}));

vi.mock('./AddArtistModal', () => ({
    AddArtistModal: () => <button>Add an artist</button>,
}));

vi.mock('../profile/ProfilePicture', () => ({
    default: () => <div data-testid="profile-picture"/>,
}));

const account = {
    id: 'account-1',
    name: 'Test account',
    email: 'test@example.com',
    emailOnNewMessage: true,
    emailUnreadDelayMinutes: 1440,
};

describe('UtilityDrawer notifications', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        vi.mocked(getMyAccount).mockResolvedValue(account);
        vi.mocked(getTopArtists).mockResolvedValue([]);
        vi.mocked(getUsersGenres).mockResolvedValue([]);
        vi.mocked(updateMyAccount).mockResolvedValue({...account, emailOnNewMessage: false});
    });

    it('loads the persisted preference and applies optimistic updates while saving', async () => {
        let resolveUpdate: ((value: typeof account) => void) | undefined;
        vi.mocked(updateMyAccount).mockReturnValue(new Promise((resolve) => {
            resolveUpdate = resolve;
        }));

        render(<UtilityDrawer/>);
        fireEvent.click(screen.getByRole('button', {name: 'Open account menu'}));

        const toggle = await screen.findByRole('checkbox', {name: 'Email me about unread messages'});
        expect(toggle).toBeChecked();

        fireEvent.click(toggle);
        expect(toggle).not.toBeChecked();
        expect(toggle).toBeDisabled();
        expect(updateMyAccount).toHaveBeenCalledWith(null, null, null, false);

        resolveUpdate?.({...account, emailOnNewMessage: false});
        await waitFor(() => expect(toggle).not.toBeDisabled());
    });

    it('rolls back the optimistic toggle when the update is rejected', async () => {
        vi.mocked(updateMyAccount).mockRejectedValueOnce(new Error('request failed'));

        render(<UtilityDrawer/>);
        fireEvent.click(screen.getByRole('button', {name: 'Open account menu'}));

        const toggle = await screen.findByRole('checkbox', {name: 'Email me about unread messages'});
        fireEvent.click(toggle);

        await waitFor(() => expect(toggle).toBeChecked());
        expect(mantineErrorNotification).toHaveBeenCalledWith(
            'Failed to update email notification preference'
        );
    });
});
