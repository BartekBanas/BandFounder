import {FC, useState} from "react";
import {useNavigate} from "react-router-dom";
import {useLoginApi} from "./api";
import './styles.css';

interface LoginFormProps{

};

export const LoginForm: FC<LoginFormProps> = ({}) => {
    const[email, changeEmail] = useState('');
    const[password, changePassword] = useState('');

    const navigate = useNavigate();

    const login = useLoginApi();

    async function handleSubmit(e : any){
        e.preventDefault();
        try{
            await login(email, password);

            await navigate('/home');
        }
        catch(e){
            console.log(e);
        }
    }

    async function handleRegisterClick(e : any){
        e.preventDefault();
        await navigate('/register');
    }

    return(
        <form id={'loginForm'} onSubmit={handleSubmit}>
            <div id="email-LoginForm">
                <label>Email</label>
                <input type="text" value={email} onChange={(e) => changeEmail(e.target.value)} required={true} placeholder={'email'}/>
            </div>
            <div id="password-LoginForm">
                <label>Password</label>
                <input type="password" value={password} onChange={(e) => changePassword(e.target.value)} required placeholder={'password'}/>
            </div>
            <div id='LoginButtonContainer'>
                <button>Login</button>
            </div>
            <button id='RegisterButton-LoginForm' onClick={handleRegisterClick}>Register</button>
        </form>
    );
};

export default LoginForm;