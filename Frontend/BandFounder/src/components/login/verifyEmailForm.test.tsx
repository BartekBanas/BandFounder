import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {MemoryRouter} from 'react-router-dom';
import {beforeEach, expect, it, vi} from 'vitest';
import {confirmEmailVerification, resendEmailVerification} from '../../api/account';
import {useIsAuthenticated} from '../../hooks/authentication';
import {mantineInformationNotification} from '../common/mantineNotification';
import {VerifyEmailForm} from './verifyEmailForm';

vi.mock('../../api/account', () => ({
    confirmEmailVerification: vi.fn(),
    resendEmailVerification: vi.fn(),
}));

vi.mock('../../hooks/authentication', () => ({
    useIsAuthenticated: vi.fn(),
}));

vi.mock('../common/mantineNotification', () => ({
    mantineErrorNotification: vi.fn(),
    mantineInformationNotification: vi.fn(),
    mantineSuccessNotification: vi.fn(),
}));

beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(confirmEmailVerification).mockResolvedValue(undefined);
    vi.mocked(useIsAuthenticated).mockReturnValue(false);
});

function renderVerificationLink() {
    render(
        <MemoryRouter initialEntries={['/verify-email?token=verification-token']}>
            <VerifyEmailForm/>
        </MemoryRouter>,
    );
}

it('requires a confirmation click when a signed-out user opens the email link', async () => {
    renderVerificationLink();

    expect(confirmEmailVerification).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', {name: 'Confirm email'}));
    expect(confirmEmailVerification).toHaveBeenCalledExactlyOnceWith('verification-token');
    expect(await screen.findByText('Your email is verified.')).toBeInTheDocument();
});

it('lets the user retry the same verification link after a temporary failure', async () => {
    vi.mocked(confirmEmailVerification).mockRejectedValueOnce(new Error('Network error'));
    renderVerificationLink();

    fireEvent.click(screen.getByRole('button', {name: 'Confirm email'}));
    expect(await screen.findByRole('alert')).toHaveTextContent('Network error');

    fireEvent.click(screen.getByRole('button', {name: 'Confirm email'}));
    expect(await screen.findByText('Your email is verified.')).toBeInTheDocument();
    expect(confirmEmailVerification).toHaveBeenCalledTimes(2);
    expect(confirmEmailVerification).toHaveBeenLastCalledWith('verification-token');
});

it('keeps a failed link separate from the signed-in account already being verified', async () => {
    vi.mocked(useIsAuthenticated).mockReturnValue(true);
    vi.mocked(confirmEmailVerification).mockRejectedValueOnce(new Error('Invalid or expired link'));
    vi.mocked(resendEmailVerification).mockResolvedValue({resendAvailableAt: null});
    renderVerificationLink();

    fireEvent.click(screen.getByRole('button', {name: 'Confirm email'}));
    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid or expired link');
    fireEvent.click(screen.getByRole('button', {name: 'Send verification email'}));

    await waitFor(() => {
        expect(mantineInformationNotification).toHaveBeenCalledExactlyOnceWith(
            'Your signed-in account already has a verified email.',
        );
    });
    expect(screen.getByRole('alert')).toHaveTextContent('Invalid or expired link');
    expect(screen.queryByText('Your email is verified.')).not.toBeInTheDocument();
});
