import {FC} from "react";
import RegisterForm from "../components/register/registerForm";

interface RegisterPageProps{

}

export const RegisterPage: FC<RegisterPageProps> = ({}) => {
    return(
        <div id = 'main'>
            <h1>Register form</h1>
            <RegisterForm/>
        </div>
    );
};