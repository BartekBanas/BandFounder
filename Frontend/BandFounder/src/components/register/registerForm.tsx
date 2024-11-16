import React, {FC, useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import {getMyAccount, registerAccount} from "../../api/account";
import {mantineInformationNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {setAuthToken, setUserId} from "../../hooks/authentication";

interface LoginFormProps {
};

export interface RegisterAccountDto {
    Name: string;
    Password: string;
    Email: string;
}

export const LoginForm: FC<LoginFormProps> = ({}) => {
    const [name, changeName] = useState('');
    const [email, changeEmail] = useState('');
    const [password, changePassword] = useState('');

    const navigate = useNavigate();

    async function handleSubmit(e: any) {
        e.preventDefault();
        try {
            const authorizationToken = await registerAccount(name, email, password);
            const account = await getMyAccount(authorizationToken);

            setAuthToken(authorizationToken);
            setUserId(account.id);

            mantineSuccessNotification('Account created successfully');

            navigate('/home');
        } catch (e) {
            console.log(e);
        }
    }

    async function handleLoginClick(e: any) {
        e.preventDefault();
        navigate('/login');
    }

    return (
        <form id='registerForm' onSubmit={handleSubmit}>
            <div id="username-RegisterForm">
                <label>Username</label>
                <input type="text" value={name} onChange={(e) => changeName(e.target.value)} required={true}
                       placeholder={'name'}/>
            </div>
            <div id="email-RegisterForm">
                <label>Email</label>
                <input type="email" value={email} onChange={(e) => changeEmail(e.target.value)} required={true}
                       placeholder={'email'}/>
            </div>
            <div id="password-RegisterForm">
                <label>Password</label>
                <input type="password" value={password} onChange={(e) => changePassword(e.target.value)} required
                       placeholder={'password'}/>
            </div>
            <div id="RegisterButtonContainer">
                <button>Register</button>
            </div>
            <button id='LoginButton-RegisterForm' onClick={handleLoginClick}>Login</button>
        </form>
    );
};

export default LoginForm;