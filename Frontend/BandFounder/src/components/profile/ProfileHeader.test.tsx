import {MantineProvider} from '@mantine/core';
import {act, fireEvent, render, screen, waitFor} from '@testing-library/react';
import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getProfilePicture, invalidateProfilePicture, uploadProfilePicture} from '../../api/account';
import {mantineErrorNotification} from '../common/mantineNotification';
import ProfileHeader from './ProfileHeader';

vi.mock('../../api/account', async (importOriginal) => ({
    ...await importOriginal<typeof import('../../api/account')>(),
    getProfilePicture: vi.fn(),
    uploadProfilePicture: vi.fn(),
}));

vi.mock('../common/mantineNotification', () => ({
    mantineErrorNotification: vi.fn(),
    mantineSuccessNotification: vi.fn(),
}));

function renderProfile() {
    render(
        <MantineProvider env="test">
            <ProfileHeader
                account={{id: 'alice-id', name: 'alice', email: 'alice@example.com'}}
                isMyProfile
                genres={[]}
                topArtists={[]}
                onMessage={vi.fn()}
                onEditProfile={vi.fn()}
            />
        </MantineProvider>,
    );
}

describe('profile header photo', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        vi.stubGlobal('matchMedia', () => ({
            matches: false,
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
        }));
        vi.mocked(getProfilePicture).mockResolvedValue('/previous-photo.png');
    });

    afterEach(() => vi.unstubAllGlobals());

    it('disables the picker during upload and lets the user retry after a failure', async () => {
        let rejectUpload!: (error: Error) => void;
        vi.mocked(uploadProfilePicture).mockReturnValueOnce(new Promise((_, reject) => {
            rejectUpload = reject;
        }));
        vi.mocked(uploadProfilePicture).mockResolvedValueOnce(undefined);
        renderProfile();
        const picker = screen.getByLabelText('Change photo');
        const photo = new File(['photo'], 'photo.png', {type: 'image/png'});

        fireEvent.change(picker, {target: {files: [photo]}});

        expect(picker).toBeDisabled();
        expect(uploadProfilePicture).toHaveBeenCalledExactlyOnceWith(photo);
        await act(async () => rejectUpload(new Error('upload failed')));
        expect(mantineErrorNotification).toHaveBeenCalledOnce();
        expect(picker).toBeEnabled();

        fireEvent.change(picker, {target: {files: [photo]}});

        await waitFor(() => expect(picker).toBeEnabled());
        expect(uploadProfilePicture).toHaveBeenCalledTimes(2);
    });

    it('refreshes the displayed photo when another uploader invalidates it', async () => {
        renderProfile();
        const photo = await screen.findByRole('img', {name: 'alice'});
        expect(photo).toHaveAttribute('src', '/previous-photo.png');

        vi.mocked(getProfilePicture).mockResolvedValue('/updated-photo.png');
        act(() => invalidateProfilePicture('alice-id'));

        await waitFor(() => expect(screen.getByRole('img', {name: 'alice'})).toHaveAttribute('src', '/updated-photo.png'));
    });
});
