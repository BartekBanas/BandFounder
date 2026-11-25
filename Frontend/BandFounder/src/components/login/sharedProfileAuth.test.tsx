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

    it.each([
        '/profile/bob',
        '/profile/Bob%20Smith',
        '/profile/Za%C5%BC%C3%B3%C5%82%C4%87',
    ])('returns to %s after logging in', async (pathname) => {
        renderAuth({pathname});
        const path = screen.getByTestId('current-path');
        const originalKey = path.getAttribute('data-location-key');

        logIn();

        await waitFor(() => expect(path).not.toHaveAttribute('data-location-key', originalKey));
        expect(path.textContent).toBe(pathname);
    });

    it('carries the shared profile through account creation', async () => {
        const pathname = '/profile/Bob%20Smith';
        renderAuth({pathname});
        fireEvent.click(screen.getByRole('button', {name: 'Create an account'}));

        fireEvent.change(screen.getByLabelText(/username/i), {target: {value: 'alice'}});
        fireEvent.change(screen.getByLabelText(/email/i), {target: {value: 'alice@example.com'}});
        fireEvent.change(screen.getByLabelText(/password/i), {target: {value: 'password'}});
        fireEvent.click(screen.getByRole('button', {name: 'Create account'}));

        await waitFor(() => expect(screen.getByTestId('current-path').textContent).toBe(pathname));
        expect(registerAccount).toHaveBeenCalledOnce();
    });

    it.each([
        '//example.com/profile/bob',
        '/profile/bob/extra',
        '/profile/%2e%2e',
        '/profile/invalid%2',
    ])('uses home when the proposed destination is %s', async (sharedProfilePath) => {
        renderAuth({pathname: '/login', state: {sharedProfilePath}});

        logIn();

        await waitFor(() => expect(screen.getByTestId('current-path')).toHaveTextContent('/home'));
    });
});
