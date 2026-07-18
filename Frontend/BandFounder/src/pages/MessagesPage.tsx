import {FC, useCallback, useState} from "react";
import {AllConversations} from "../components/messeges/AllConversation/allConversations";
import {SelectedConversation} from "../components/messeges/SelectedConversation/selectedConversation";
import {useParams} from "react-router-dom";
import {useUnreadMessages} from "../hooks/useUnreadMessages";
import {getUserId} from "../hooks/authentication";
import './styles/MessagesPage.css'

export const MessagesPage: FC = () => {
    const {id: selectedId = ''} = useParams<{ id: string }>();
    const [activity, setActivity] = useState<{ chatroomId: string; sentDate: string } | null>(null);
    const {bumpRoom, clearRoom} = useUnreadMessages();
    const myId = getUserId();

    const handleConversationActivity = useCallback((
        chatroomId: string,
        sentDate: string,
        senderId?: string
    ) => {
        setActivity({chatroomId, sentDate});

        if (senderId && senderId === myId) {
            return;
        }

        if (chatroomId === selectedId) {
            clearRoom(chatroomId);
            return;
        }

        bumpRoom(chatroomId);
    }, [bumpRoom, clearRoom, myId, selectedId]);

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
