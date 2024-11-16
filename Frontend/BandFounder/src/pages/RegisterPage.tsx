import {FC} from "react";
import RegisterForm from "../components/register/registerForm";

interface RegisterPageProps {
}

export const RegisterPage: FC<RegisterPageProps> = ({}) => {
    return (
        <RegisterForm/>
    );
};