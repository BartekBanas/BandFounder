import { FC, useEffect, useState } from "react";
import { Message } from "../../../types/Message";
import {getSelectedChatroom, getSelectedConversation, sendMessage} from "./api";
import {getCurrentUser, getUserById} from "../../common/frequentlyUsed";
import {TextField, IconButton, ThemeProvider} from "@mui/material";
import SendIcon from "@mui/icons-material/Send";

interface SelectedConversationProps {
    id: string;
}

interface MessageWithSenderName extends Message {
    senderName: string;
}

export const SelectedConversation: FC<SelectedConversationProps> = ({ id }) => {
    const [currentConversation, setCurrentConversation] = useState<MessageWithSenderName[]>([]);
    const [newMessage, setNewMessage] = useState<string>("");
    const [receiverName, setReceiverName] = useState<string>("");
    const [reload, setReload] = useState(0);



    useEffect(() => {
        const fetchConversation = async () => {
            if (id) {
                try {
                    const conversation: Message[] = await getSelectedConversation(id);

                    const conversationWithNames = await Promise.all(
                        conversation.map(async (message) => {
                            const user = await getUserById(message.senderId);
                            const currentUser = await getCurrentUser();
                            if(currentUser.id === message.senderId) {
                                return {
                                    ...message,
                                    senderName: "You",
                                };
                            }
                            else {
                                setReceiverName(user?.name || "Unknown User");
                            }
                            return {
                                ...message,
                                senderName: user?.name || "Unknown User",
                            };
                        })
                    );

                    setCurrentConversation(conversationWithNames);
                } catch (error) {
                    console.error("Error fetching conversation:", error);
                }
            }
        };
        const fetchCurrentChatroom = async () => {
            try {
                if (id) {

                    const chatroom = await getSelectedChatroom(id);
                    if (chatroom && chatroom.membersIds && chatroom.membersIds.length > 0) {
                        const user = await getUserById(chatroom.membersIds[0]);
                        const currentUser = await getCurrentUser();
                        let reciverId:string;
                        if (currentUser.id === chatroom.membersIds[0]) {
                            reciverId = chatroom.membersIds[1];
                        }
                        else{
                            reciverId = chatroom.membersIds[0];
                        }
                        const receiver = await getUserById(reciverId);
                        if (receiver) {
                            setReceiverName(receiver.name);
                        }
                    }
                }
            } catch (e) {
                console.error('Error getting selected chatroom:', e);
            }
        };

        fetchConversation();
        fetchCurrentChatroom();
    }, [id, receiverName,reload]);

    const handleSendMessage = async () => {
        if (newMessage.trim()) {
            await sendMessage(id, newMessage.trim());
            setNewMessage("");
            setReload(reload+1);
        }
    };

    const handleKeyPress = (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (event.key === "Enter") {
            event.preventDefault();
            handleSendMessage();
        }
    };

    return (
            <div id="main">
                <h1>Conversation with {receiverName}</h1>
                <ul>
                    {currentConversation.map((message, index) => (
                        <li key={index}>
                            {message.senderName} : {message.content}
                        </li>
                    ))}
                </ul>

                <div style={{display: "flex", alignItems: "center", marginTop: "1rem"}}>
                    <TextField
                        fullWidth
                        variant="outlined"
                        label="Type a message"
                        value={newMessage}
                        onChange={(e) => setNewMessage(e.target.value)}
                        onKeyPress={handleKeyPress}
                        sx={{"width": "30%"}}
                    />
                    <IconButton color="primary" onClick={handleSendMessage}>
                        <SendIcon/>
                    </IconButton>
                </div>
            </div>
    );
};
