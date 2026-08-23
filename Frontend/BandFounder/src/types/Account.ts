export interface Account {
    id: string;
    name: string;
    email: string;
    emailOnNewMessage?: boolean;
    emailUnreadDelayMinutes?: number;
}

export interface PasswordResetInfo {
    accountId: string;
    username: string;
    email: string;
    hasProfilePicture: boolean;
    memberSince: string;
    expiresAt: string;
}

export interface CreateAccount {
    Name: string;
    Password: string;
    Email: string;
}