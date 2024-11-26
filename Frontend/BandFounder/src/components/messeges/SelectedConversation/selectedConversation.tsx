import { FC, useEffect, useRef, useState, useCallback } from "react";
import { Message } from "../../../types/Message";
import { TextField, IconButton, Tooltip, CircularProgress } from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import "./styles.css";
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './../../../assets/CustomScrollbar.css';
import { getChatroom } from "../../../api/chatroom";
import { getMessagesFromChatroom, sendMessage } from "../../../api/messages";
import { getAccount } from "../../../api/account";
import { getUserId } from "../../../hooks/authentication";

interface SelectedConversationProps {
    id: string;
}

interface MessageWithSenderName extends Message {
    senderName: string;
    timeSinceSent: string;
}

export const SelectedConversation: FC<SelectedConversationProps> = ({ id }) => {
    const [currentConversation, setCurrentConversation] = useState<MessageWithSenderName[]>([]);
    const [newMessage, setNewMessage] = useState<string>("");
    const [receiverName, setReceiverName] = useState<string>("Unknown User");
    const [loading, setLoading] = useState<boolean>(false);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(10);
    const [hasMore, setHasMore] = useState<boolean>(true);
    const bottomRef = useRef<HTMLDivElement>(null);
    const topRef = useRef<HTMLDivElement>(null);
    const [isScrollingUp, setIsScrollingUp] = useState<boolean>(false);

    useEffect(() => {
        const fetchOlderMessages = async () => {
            if (id && hasMore && pageNumber > 1) {
                setLoading(true);
                try {
                    const conversation: Message[] = await getMessagesFromChatroom(id, pageNumber, pageSize);
                    const userId = getUserId();

                    const conversationWithNames = await Promise.all(
                        conversation.map(async (message) => {
                            const user = await getAccount(message.senderId);
                            const senderName = userId === message.senderId ? "You" : user.name || "Unknown User";

                            // Calculate time since the message was sent
                            const messageTime = new Date(message.sentDate);
                            const currentTime = new Date();
                            const timeDifference = Math.floor((currentTime.getTime() - messageTime.getTime()) / 1000);

                            let timeSinceSent;
                            if (timeDifference < 60) {
                                timeSinceSent = `${timeDifference} second${timeDifference === 1 ? "" : "s"} ago`;
                            } else if (timeDifference < 3600) {
                                const minutes = Math.floor(timeDifference / 60);
                                timeSinceSent = `${minutes} minute${minutes === 1 ? "" : "s"} ago`;
                            } else if (timeDifference < 86400) {
                                const hours = Math.floor(timeDifference / 3600);
                                timeSinceSent = `${hours} hour${hours === 1 ? "" : "s"} ago`;
                            } else {
                                const days = Math.floor(timeDifference / 86400);
                                timeSinceSent = `${days} day${days === 1 ? "" : "s"} ago`;
                            }

                            return {
                                ...message,
                                senderName,
                                timeSinceSent,
                            };
                        })
                    );

                    // Add fetched messages to the conversation
                    setCurrentConversation((prev) => [...conversationWithNames, ...prev]);

                    // Update `hasMore` to false if the last page is reached
                    setHasMore(conversation.length === pageSize);
                } catch (error) {
                    console.error("Error fetching older messages:", error);
                } finally {
                    setLoading(false);
                }
            }
        };

        fetchOlderMessages();
    }, [id, pageNumber]);

    useEffect(() => {
        if (!isScrollingUp && bottomRef.current) {
            bottomRef.current.scrollIntoView({ behavior: 'smooth' });
        } else if (isScrollingUp && topRef.current) {
            topRef.current.scrollIntoView({ behavior: 'auto', block: 'end' });
        }
    }, [currentConversation, isScrollingUp]);

    useEffect(() => {
        const observer = new IntersectionObserver(
            (entries) => {
                if (entries[0].isIntersecting && hasMore && !loading) {
                    setIsScrollingUp(true);
                    setPageNumber((prevPageNumber) => prevPageNumber + 1);
                }
            },
            { threshold: 1.0 }
        );

        if (topRef.current) observer.observe(topRef.current);

        return () => {
            setIsScrollingUp(false);
            observer.disconnect();
        };
    }, [hasMore, loading]);

    const handleSendMessage = async () => {
        if (newMessage.trim()) {
            try {
                await sendMessage(id, newMessage.trim());
                setNewMessage("");
                setPageNumber(1);
                setCurrentConversation([]);
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
            <ul id="fullConversation" className="custom-scrollbar" style={{ listStyleType: "none", padding: 0 }}>
                {loading && <CircularProgress />}
                <div ref={topRef} />
                {currentConversation.map((message, index) => (
                    <li
                        key={index}
                        style={{
                            textAlign: message.senderName === "You" ? "right" : "left",
                            margin: "10px 0",
                        }}
                    >
                        {message.senderName === "You" ? (
                            <div className="singleMessageYou">
                                <Tooltip title={`${message.senderName}`}>
                                    <div className="messageProfilePresentation">
                                        <img src={defaultProfileImage} alt="defaultProfileImage" />
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className="messageContent">{message.content}</div>
                                </Tooltip>
                            </div>
                        ) : (
                            <div className="singleMessageThey">
                                <Tooltip title={`${message.senderName}`}>
                                    <div className="messageProfilePresentation">
                                        <img src={defaultProfileImage} alt="defaultProfileImage" />
                                    </div>
                                </Tooltip>
                                <Tooltip title={`Sent ${message.timeSinceSent}`}>
                                    <div className="messageContent">{message.content}</div>
                                </Tooltip>
                            </div>
                        )}
                    </li>
                ))}
                <div ref={bottomRef} />
            </ul>
            <div id="sendBox" style={{ display: "flex", alignItems: "center", marginTop: "1rem" }}>
                <TextField
                    fullWidth
                    variant="outlined"
                    label="Type a message"
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    onKeyPress={handleKeyPress}
                    inputProps={{ "aria-label": "Type a message" }}
                />
                <IconButton color="primary" onClick={handleSendMessage} aria-label="Send message">
                    <SendIcon />
                </IconButton>
            </div>
        </div>
    );
};