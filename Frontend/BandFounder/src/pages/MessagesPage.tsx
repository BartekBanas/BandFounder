// src/pages/MessagesPage.tsx
import { FC, useEffect, useState } from "react";
import { AllConversations } from "../components/messeges/AllConversation/allConversations";
import { SelectedConversation } from "../components/messeges/SelectedConversation/selectedConversation";
import { muiDarkTheme } from "../assets/muiDarkTheme";
import { ThemeProvider } from "@mui/material";
import {useParams} from "react-router-dom";

interface MessagesPageProps {
}

export const MessagesPage: FC<MessagesPageProps> = ({}) => {
    const { id: paramId } = useParams<{ id: string }>();
    const [selectedConversationId, setSelectedConversationId] = useState<string | undefined>('');

    useEffect(() => {
        setSelectedConversationId(paramId);
    }, [paramId]);

    return (
        <div id="main">
            <AllConversations onSelectConversation={setSelectedConversationId}/>
            <SelectedConversation id={selectedConversationId || ''}/>
        </div>
    );
};