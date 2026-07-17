import {FC, useCallback, useState} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import {SelectedConversation} from "../components/messeges/SelectedConversation/selectedConversation";
import {useParams} from "react-router-dom";
import './styles/MessagesPage.css'

export const MessagesPage: FC = () => {
    const {id: selectedId = ''} = useParams<{ id: string }>();
    const [activity, setActivity] = useState<{ chatroomId: string; sentDate: string } | null>(null);

    const handleConversationActivity = useCallback((chatroomId: string, sentDate: string) => {
        setActivity({chatroomId, sentDate});
    }, []);

    return (
        <div id="messagesPage">
            <AllConversations selectedId={selectedId} activity={activity}/>
            <SelectedConversation
                key={selectedId}
                id={selectedId}
                onConversationActivity={handleConversationActivity}
            />
        </div>
    );
};