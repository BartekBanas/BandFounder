import {fireEvent, render, screen} from '@testing-library/react';
import {MemoryRouter} from 'react-router-dom';
import {expect, it, vi} from 'vitest';
import {confirmEmailVerification} from '../../api/account';
import {VerifyEmailForm} from './verifyEmailForm';

vi.mock('../../api/account', () => ({
    confirmEmailVerification: vi.fn().mockResolvedValue(undefined),
    resendEmailVerification: vi.fn(),
}));

vi.mock('../../hooks/authentication', () => ({
    useIsAuthenticated: () => false,
}));

it('requires a confirmation click when a signed-out user opens the email link', async () => {
    render(
        <MemoryRouter initialEntries={['/verify-email?token=verification-token']}>
            <VerifyEmailForm/>
        </MemoryRouter>,
    );

    expect(confirmEmailVerification).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', {name: 'Confirm email'}));
    expect(confirmEmailVerification).toHaveBeenCalledExactlyOnceWith('verification-token');
    expect(await screen.findByText('Your email is verified.')).toBeInTheDocument();
});
