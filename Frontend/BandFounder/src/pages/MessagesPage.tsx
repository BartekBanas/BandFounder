import {FC, useEffect, useState} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import {SelectedConversation} from "../components/messeges/SelectedConversation/selectedConversation";
import {useParams} from "react-router-dom";
import './styles/MessagesPage.css'

export const MessagesPage: FC = () => {
    const {id: paramId} = useParams<{ id: string }>();
    const [selectedConversationId, setSelectedConversationId] = useState('');

    useEffect(() => {
        setSelectedConversationId(paramId ?? '');
    }, [paramId]);

    return (
        <div id="messagesPage">
            <AllConversations onSelectConversation={setSelectedConversationId}/>
            <SelectedConversation id={selectedConversationId}/>
        </div>
    );
};