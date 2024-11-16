import {FC, useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import {getMyAccount, login} from "../../api/account";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {mantineInformationNotification} from "../common/mantineNotification";

interface LoginFormProps {
};

export const useLoginApi = () => {
    return async (usernameOrEmail: string, password: string) => {
        const authorizationToken = await login(usernameOrEmail, password);
        setAuthToken(authorizationToken);

        const account = await getMyAccount(authorizationToken);
        setUserId(account.id);

        mantineInformationNotification(`Welcome ${account.name}`);
    };
};

export const LoginForm: FC<LoginFormProps> = ({}) => {
    const [email, changeEmail] = useState('');
    const [password, changePassword] = useState('');

    const navigate = useNavigate();

    const login = useLoginApi();

    async function handleSubmit(e: any) {
        e.preventDefault();
        try {
            await login(email, password);

            navigate('/home');
        } catch (e) {
            console.log(e);
        }
    }

    async function handleRegisterClick(e: any) {
        e.preventDefault();
        navigate('/register');
    }

    return (
        <form id={'loginForm'} onSubmit={handleSubmit}>
            <div id="email-LoginForm">
                <label>Email</label>
                <input type="text" value={email} onChange={(e) => changeEmail(e.target.value)} required={true}
                       placeholder={'email'}/>
            </div>
            <div id="password-LoginForm">
                <label>Password</label>
                <input type="password" value={password} onChange={(e) => changePassword(e.target.value)} required
                       placeholder={'password'}/>
            </div>
            <div id='LoginButtonContainer'>
                <button>Login</button>
            </div>
            <button id='RegisterButton-LoginForm' onClick={handleRegisterClick}>Register</button>
        </form>
    );
};

export default LoginForm;