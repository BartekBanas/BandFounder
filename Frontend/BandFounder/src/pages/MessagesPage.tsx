import {FC} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import {SelectedConversation} from "../components/messeges/SelectedConversation/selectedConversation";
import {useParams} from "react-router-dom";
import './styles/MessagesPage.css'

export const MessagesPage: FC = () => {
    const {id: selectedId = ''} = useParams<{ id: string }>();

    return (
        <div id="messagesPage">
            <AllConversations selectedId={selectedId}/>
            <SelectedConversation key={selectedId} id={selectedId}/>
        </div>
    );
};