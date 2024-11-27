import {FC, useEffect, useState} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import {SelectedConversation} from "../components/messeges/SelectedConversation/selectedConversation";
import {useParams} from "react-router-dom";
import './styles/MessagesPage.css'

interface MessagesPageProps {
}

export const MessagesPage: FC<MessagesPageProps> = ({}) => {
    const {id: paramId} = useParams<{ id: string }>();
    const [selectedConversationId, setSelectedConversationId] = useState<string | undefined>('');

    useEffect(() => {
        setSelectedConversationId(paramId);
    }, [paramId]);

    return (
        <div id="mainMessagingPage">
            <AllConversations onSelectConversation={setSelectedConversationId}/>
            <SelectedConversation id={selectedConversationId || ''}/>
        </div>
    );
};