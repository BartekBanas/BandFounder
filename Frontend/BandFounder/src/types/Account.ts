export interface Account {
    id: string;
    name: string;
    email: string;
}

export interface CreateAccountDto {
    Name: string;
    Password: string;
    Email: string;
}