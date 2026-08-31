import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import type {AccountSettings} from '../../types/Account';
import {EmailVerificationPrompt} from './EmailVerificationPrompt';

const {
    getMyAccountMock,
    resendEmailVerificationMock,
    successNotificationMock,
} = vi.hoisted(() => ({
    getMyAccountMock: vi.fn(),
    resendEmailVerificationMock: vi.fn(),
    successNotificationMock: vi.fn(),
}));

vi.mock('../../api/account', () => ({
    EmailVerificationResendError: class extends Error {
        constructor(public readonly resendAvailableAt: string | null) {
            super('Please wait before requesting another verification email.');
        }
    },
    getMyAccount: getMyAccountMock,
    resendEmailVerification: resendEmailVerificationMock,
}));

vi.mock('../common/mantineNotification', () => ({
    mantineErrorNotification: vi.fn(),
    mantineSuccessNotification: successNotificationMock,
}));

function accountSettings(overrides: Partial<AccountSettings> = {}): AccountSettings {
    return {
        id: 'account-1',
        name: 'Test account',
        email: 'member@example.com',
        emailOnNewMessage: true,
        emailUnreadDelayMinutes: 1440,
        emailVerified: false,
        resendAvailableAt: null,
        ...overrides,
    };
}

describe('EmailVerificationPrompt', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        sessionStorage.clear();
    });

    it('shows the initial email as sent and withholds resend during cooldown', async () => {
        getMyAccountMock.mockResolvedValue(accountSettings({
            resendAvailableAt: new Date(Date.now() + 60_000).toISOString(),
        }));

        render(<EmailVerificationPrompt/>);

        expect(await screen.findByText(/We sent a verification email when you registered/))
            .toBeInTheDocument();
        expect(screen.getByText(/You can resend it in/)).toBeInTheDocument();
        expect(screen.queryByRole('button', {name: 'Resend email'})).not.toBeInTheDocument();
    });

    it('offers resend when server timing allows and applies the returned cooldown', async () => {
        getMyAccountMock.mockResolvedValue(accountSettings({
            resendAvailableAt: new Date(Date.now() - 1_000).toISOString(),
        }));
        resendEmailVerificationMock.mockResolvedValue({
            resendAvailableAt: new Date(Date.now() + 60_000).toISOString(),
        });

        render(<EmailVerificationPrompt/>);

        fireEvent.click(await screen.findByRole('button', {name: 'Resend email'}));

        await waitFor(() => {
            expect(resendEmailVerificationMock).toHaveBeenCalledOnce();
            expect(successNotificationMock)
                .toHaveBeenCalledWith('Verification email queued for delivery');
            expect(screen.queryByRole('button', {name: 'Resend email'})).not.toBeInTheDocument();
        });
        expect(screen.getByText(/You can resend it in/)).toBeInTheDocument();
    });

    it('does not carry a dismissal across logout and login as another account', async () => {
        getMyAccountMock.mockResolvedValueOnce(accountSettings({
            email: 'first@example.com',
        }));

        const firstSession = render(<EmailVerificationPrompt/>);
        fireEvent.click(await screen.findByRole('button', {name: 'Close'}));
        await waitFor(() => {
            expect(screen.queryByText('Verify your email')).not.toBeInTheDocument();
        });
        firstSession.unmount();

        getMyAccountMock.mockResolvedValueOnce(accountSettings({
            id: 'account-2',
            email: 'second@example.com',
        }));
        render(<EmailVerificationPrompt/>);

        expect(await screen.findByText('Verify your email')).toBeInTheDocument();
    });

    it('does not carry a dismissal across an email change on the same account', async () => {
        getMyAccountMock.mockResolvedValueOnce(accountSettings({
            email: 'old@example.com',
        }));

        const beforeEmailChange = render(<EmailVerificationPrompt/>);
        fireEvent.click(await screen.findByRole('button', {name: 'Close'}));
        beforeEmailChange.unmount();

        getMyAccountMock.mockResolvedValueOnce(accountSettings({
            email: ' NEW@EXAMPLE.COM ',
        }));
        render(<EmailVerificationPrompt/>);

        expect(await screen.findByText('Verify your email')).toBeInTheDocument();
    });

    it('closes a stale prompt after verification status changes', async () => {
        getMyAccountMock
            .mockResolvedValueOnce(accountSettings())
            .mockResolvedValueOnce(accountSettings({
                emailVerified: true,
            }));

        render(<EmailVerificationPrompt/>);
        expect(await screen.findByText('Verify your email')).toBeInTheDocument();

        fireEvent.focus(window);

        await waitFor(() => {
            expect(screen.queryByText('Verify your email')).not.toBeInTheDocument();
        });
    });
});
