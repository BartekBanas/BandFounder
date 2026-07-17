import {render, screen, waitFor} from '@testing-library/react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import {MemoryRouter, Route, Routes} from 'react-router-dom';
import {ProfilePage} from './ProfilePage';

const {getAccountMock, getAccountByUsernameMock, getUserIdMock} = vi.hoisted(() => ({
    getAccountMock: vi.fn(),
    getAccountByUsernameMock: vi.fn(),
    getUserIdMock: vi.fn(),
}));

vi.mock('../api/account', () => ({
    getAccount: getAccountMock,
    getAccountByUsername: getAccountByUsernameMock,
}));

vi.mock('../hooks/authentication', () => ({
    getUserId: getUserIdMock,
}));

vi.mock('../components/common/AppLoader', () => ({
    AppLoader: () => <div data-testid="app-loader"/>,
}));

vi.mock('../components/profile/ProfileShow', () => ({
    default: ({isMyProfile}: {isMyProfile: boolean}) => (
        <div data-testid="profile-show">{isMyProfile ? 'own' : 'other'}</div>
    ),
}));

vi.mock('../components/listing/listingOwner/listingsListPrivate', () => ({
    default: () => <div data-testid="listings-private"/>,
}));

vi.mock('../components/listing/listingProfilePublic/listingsListProfilePublic', () => ({
    default: () => <div data-testid="listings-public"/>,
}));

const renderProfilePage = (username: string) =>
    render(
        <MemoryRouter initialEntries={[`/profile/${username}`]}>
            <Routes>
                <Route path="/profile/:username" element={<ProfilePage/>}/>
            </Routes>
        </MemoryRouter>
    );

describe('ProfilePage', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getUserIdMock.mockReturnValue('user-1');
    });

    it('renders private listings when viewing your own profile', async () => {
        getAccountByUsernameMock.mockResolvedValue({id: 'user-1', name: 'alice'});
        getAccountMock.mockResolvedValue({id: 'user-1', name: 'alice'});

        renderProfilePage('alice');

        await waitFor(() => {
            expect(screen.getByTestId('profile-show')).toHaveTextContent('own');
            expect(screen.getByTestId('listings-private')).toBeInTheDocument();
        });
        expect(screen.queryByTestId('listings-public')).not.toBeInTheDocument();
    });

    it('renders public listings when viewing another user profile', async () => {
        getAccountByUsernameMock.mockResolvedValue({id: 'user-2', name: 'bob'});
        getAccountMock.mockResolvedValue({id: 'user-1', name: 'alice'});

        renderProfilePage('bob');

        await waitFor(() => {
            expect(screen.getByTestId('profile-show')).toHaveTextContent('other');
            expect(screen.getByTestId('listings-public')).toBeInTheDocument();
        });
        expect(screen.queryByTestId('listings-private')).not.toBeInTheDocument();
    });
});
