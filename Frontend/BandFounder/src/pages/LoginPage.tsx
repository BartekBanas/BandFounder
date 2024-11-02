import {FC} from "react";
import {LoginForm} from "../components/login/loginForm";

interface LoginPageProps{

};

export const LoginPage: FC<LoginPageProps> = ({}) => {
    return(
        <div id='main'>
            <h1>Login form</h1>
            <LoginForm/>
        </div>
    );
};