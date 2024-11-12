import React, {FC, useState} from "react";
import {useNavigate} from "react-router-dom";
import {useRegisterApi} from "./api";
import './styles.css';

interface LoginFormProps{

};

export const LoginForm: FC<LoginFormProps> = ({}) => {
    const[name, changeName] = useState('');
    const[email, changeEmail] = useState('');
    const[password, changePassword] = useState('');

    const navigate = useNavigate();
    const register = useRegisterApi();

    async function handleSubmit(e : any){
        e.preventDefault();
        try{
            await register(name, email, password);
            await navigate('/home');
        }
        catch(e){
            console.log(e);
        }
    }

    async function handleLoginClick(e : any){
        e.preventDefault();
        navigate('/login');
    }

    return(
        <form id='registerForm' onSubmit={handleSubmit}>
            <div id="username-RegisterForm">
                <label>Username</label>
                <input type="text" value={name} onChange={(e) => changeName(e.target.value)} required={true} placeholder={'name'}/>
            </div>
            <div id="email-RegisterForm">
                <label>Email</label>
                <input type="email" value={email} onChange={(e) => changeEmail(e.target.value)} required={true} placeholder={'email'}/>
            </div>
            <div id="password-RegisterForm">
                <label>Password</label>
                <input type="password" value={password} onChange={(e) => changePassword(e.target.value)} required placeholder={'password'}/>
            </div>
            <div id="RegisterButtonContainer">
                <button>Register</button>
            </div>
            <button id='LoginButton-RegisterForm' onClick={handleLoginClick}>Login</button>
        </form>
    );
};

export default LoginForm;