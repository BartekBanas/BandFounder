import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {MemoryRouter, Route, Routes, useLocation} from 'react-router-dom';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import {getMyAccount, login, registerAccount} from '../../api/account';
import {LoginForm} from './loginForm';
import {LoginForm as RegisterForm} from '../register/registerForm';

vi.mock('../../api/account', () => ({
    getMyAccount: vi.fn(),
    login: vi.fn(),
    registerAccount: vi.fn(),
}));

vi.mock('../../hooks/authentication', () => ({
    setAuthToken: vi.fn(),
    setUserId: vi.fn(),
}));

vi.mock('../common/mantineNotification', () => ({
    mantineErrorNotification: vi.fn(),
    mantineInformationNotification: vi.fn(),
    mantineSuccessNotification: vi.fn(),
}));

function CurrentPath() {
    const {pathname, key} = useLocation();
    return <output data-testid="current-path" data-location-key={key}>{pathname}</output>;
}

function renderAuth(entry: {pathname: string; state?: {sharedProfilePath: string}}) {
    render(
        <MemoryRouter initialEntries={[entry]}>
            <CurrentPath/>
            <Routes>
                <Route path="/register" element={<RegisterForm/>}/>
                <Route path="*" element={<LoginForm/>}/>
            </Routes>
        </MemoryRouter>,
    );
}

function logIn() {
    fireEvent.change(screen.getByLabelText(/email or username/i), {target: {value: 'alice'}});
    fireEvent.change(screen.getByLabelText(/password/i), {target: {value: 'password'}});
    fireEvent.click(screen.getByRole('button', {name: 'Log in'}));
}

describe('authentication from a shared profile', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        vi.mocked(login).mockResolvedValue('token');
        vi.mocked(registerAccount).mockResolvedValue('token');
        vi.mocked(getMyAccount).mockResolvedValue({
            id: 'alice-id',
            name: 'alice',
            email: 'alice@example.com',
            emailOnNewMessage: true,
            emailUnreadDelayMinutes: 60,
            emailVerified: false,
            resendAvailableAt: null,
        });
    });

    it('returns to the shared profile after logging in', async () => {
        renderAuth({pathname: '/profile/bob'});
        const path = screen.getByTestId('current-path');
        const originalKey = path.getAttribute('data-location-key');

        logIn();

        await waitFor(() => expect(path).not.toHaveAttribute('data-location-key', originalKey));
        expect(path).toHaveTextContent('/profile/bob');
    });

    it('carries the shared profile through account creation', async () => {
        renderAuth({pathname: '/profile/bob'});
        fireEvent.click(screen.getByRole('button', {name: 'Create an account'}));

        fireEvent.change(screen.getByLabelText(/username/i), {target: {value: 'alice'}});
        fireEvent.change(screen.getByLabelText(/email/i), {target: {value: 'alice@example.com'}});
        fireEvent.change(screen.getByLabelText(/password/i), {target: {value: 'password'}});
        fireEvent.click(screen.getByRole('button', {name: 'Create account'}));

        await waitFor(() => expect(screen.getByTestId('current-path')).toHaveTextContent('/profile/bob'));
        expect(registerAccount).toHaveBeenCalledOnce();
    });

    it('uses home when the proposed destination is outside the profile route', async () => {
        renderAuth({pathname: '/login', state: {sharedProfilePath: '//example.com/profile/bob'}});

        logIn();

        await waitFor(() => expect(screen.getByTestId('current-path')).toHaveTextContent('/home'));
    });
});
