import {FC, useEffect, useRef, useState} from "react";
import {Message} from "../../../types/Message";
import {getCurrentUser, getUserById} from "../../common/frequentlyUsed";
import {TextField, IconButton, Tooltip} from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import "./styles.css";
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './../../../assets/CustomScrollbar.css'
import {getChatroom} from "../../../api/chatroom";
import {getMessagesFromChatroom, sendMessage} from "../../../api/messages";

interface SelectedConversationProps {
    id: string;
}

interface MessageWithSenderName extends Message {
    senderName: string;
    timeSinceSent: string;
}

export const SelectedConversation: FC<SelectedConversationProps> = ({id}) => {
    const [currentConversation, setCurrentConversation] = useState<MessageWithSenderName[]>([]);
    const [newMessage, setNewMessage] = useState<string>("");
    const [receiverName, setReceiverName] = useState<string>("Unknown User");
    const [reload, setReload] = useState(0);
    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const fetchConversation = async () => {
            if (id) {
                try {
                    const conversation: Message[] = await getMessagesFromChatroom(id);
                    const currentUser = await getCurrentUser();

                    const conversationWithNames = await Promise.all(
                        conversation.map(async (message) => {
                            const user = await getUserById(message.senderId);
                            const senderName = currentUser.id === message.senderId ? "You" : user?.name || "Unknown User";

                            // Calculate time since the message was sent
                            const messageTime = new Date(message.sentDate);
                            const currentTime = new Date();
                            const timeDifference = Math.floor((currentTime.getTime() - messageTime.getTime()) / 1000); // in seconds

                            let timeSinceSent;
                            if (timeDifference < 60) {
                                timeSinceSent = `${timeDifference} ${timeDifference === 1 ? 'second' : 'seconds'} ago`;
                            } else if (timeDifference < 3600) {
                                const minutes = Math.floor(timeDifference / 60);
                                timeSinceSent = `${minutes} ${minutes === 1 ? 'minute' : 'minutes'} ago`;
                            } else if (timeDifference < 86400) {
                                const hours = Math.floor(timeDifference / 3600);
                                timeSinceSent = `${hours} ${hours === 1 ? 'hour' : 'hours'} ago`;
                            } else {
                                const days = Math.floor(timeDifference / 86400);
                                timeSinceSent = `${days} ${days === 1 ? 'day' : 'days'} ago`;
                            }

                            return {
                                ...message,
                                senderName,
                                timeSinceSent,
                            };
                        })
                    );

                    setCurrentConversation(conversationWithNames);
                } catch (error) {
                    console.error("Error fetching conversation:", error);
                }
            }
        };

        const fetchReceiverName = async () => {
            try {
                if (id) {
                    const chatroom = await getChatroom(id);
                    const currentUser = await getCurrentUser();
                    const receiverId = chatroom.membersIds.find((memberId: string) => memberId !== currentUser.id);

                    if (receiverId) {
                        const receiver = await getUserById(receiverId);
                        setReceiverName(receiver?.name || "Unknown User");
                    }
                }
            } catch (error) {
                console.error("Error fetching receiver name:", error);
            }
        };

        fetchConversation();
        fetchReceiverName();
    }, [id, reload]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({behavior: "smooth"});
    }, [currentConversation]);

    const handleSendMessage = async () => {
        if (newMessage.trim()) {
            try {
                await sendMessage(id, newMessage.trim());
                setNewMessage("");
                setReload((prev) => prev + 1);
            } catch (error) {
                console.error("Error sending message:", error);
            }
        }
    };

    const handleKeyPress = (event: React.KeyboardEvent<HTMLInputElement>) => {
        if (event.key === "Enter") {
            event.preventDefault();
            handleSendMessage();
        }
    };

    return (
        <div id="mainSelectedConversation">
            <h1 id="selectedConversationTitle">Conversation with {receiverName}</h1>
            <ul id="fullConversation" className={'custom-scrollbar'} style={{listStyleType: "none", padding: 0}}>
                {currentConversation.map((message, index) => (
                    <li
                        key={index}
                        style={{
                            textAlign: message.senderName === "You" ? "right" : "left",
                            margin: "10px 0",
                        }}
                    >
                        {message.senderName === "You" ? (
                            <div className={'singleMessageYou'}>
                                <Tooltip title={`${message.senderName}`}>
                                    <div className={'messageProfilePresentation'}>
                                        <img src={defaultProfileImage} alt="defaultProfileImage"/>
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className={'messageContent'}>
                                        {message.content}
                                    </div>
                                </Tooltip>
                            </div>
                        ) : (
                            <div className={'singleMessageThey'}>
                                <Tooltip title={`${message.senderName}`}>
                                    <div className={'messageProfilePresentation'}>
                                        <img src={defaultProfileImage} alt="defaultProfileImage"/>
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className={'messageContent'}>
                                        {message.content}
                                    </div>
                                </Tooltip>
                            </div>
                        )}
                    </li>
                ))}
                <div ref={bottomRef}/>
            </ul>
            <div id="sendBox" style={{display: "flex", alignItems: "center", marginTop: "1rem"}}>
                <TextField
                    fullWidth
                    variant="outlined"
                    label="Type a message"
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    onKeyPress={handleKeyPress}
                    inputProps={{"aria-label": "Type a message"}}
                />
                <IconButton
                    color="primary"
                    onClick={handleSendMessage}
                    aria-label="Send message"
                >
                    <SendIcon/>
                </IconButton>
            </div>
        </div>
    );
};
