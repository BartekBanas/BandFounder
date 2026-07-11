import {FC, useState} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import {SelectedConversation} from "../components/messeges/SelectedConversation/selectedConversation";
import './styles/MessagesPageMain.css'

interface MessagesPageMainProps {
}

export const MessagesPageMain: FC<MessagesPageMainProps> = ({}) => {
    const [selectedConversationId, setSelectedConversationId] = useState<string>('');

    return (
        <div id="messagesPageMain" className={selectedConversationId ? 'messages-layout--thread-open' : ''}>
            <AllConversations onSelectConversation={setSelectedConversationId}/>
            <SelectedConversation id={selectedConversationId}/>
        </div>
    );
};
