import {FC, useState} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import './styles/MessagesPageMain.css'

interface MessagesPageMainProps {
}

export const MessagesPageMain: FC<MessagesPageMainProps> = ({}) => {
    const [selectedConversationId, setSelectedConversationId] = useState<string>('');

    return (
        <div id="messagesPageMain">
            <AllConversations onSelectConversation={setSelectedConversationId}/>
        </div>
    );
};