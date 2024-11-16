import {FC} from "react";
import RegisterForm from "../components/register/registerForm";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";

interface MessagesPageProps{

}

export const MessagesPage: FC<MessagesPageProps> = ({}) => {
    return(
        <div id = 'main'>
            <AllConversations/>
        </div>
    );
};