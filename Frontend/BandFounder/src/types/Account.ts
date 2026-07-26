export interface Account {
    id: string;
    name: string;
    email: string;
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