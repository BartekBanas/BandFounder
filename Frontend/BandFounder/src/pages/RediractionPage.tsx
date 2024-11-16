import React, { FC, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { CircularProgress } from "@mui/material";

interface RediractionPageProps {}

export const RediractionPage: FC<RediractionPageProps> = ({}) => {
    const navigate = useNavigate();
    const [dots, setDots] = React.useState("");
    useEffect(() => {
        const timer = setTimeout(() => {
            navigate("/home");
        }, 2000); // 2 seconds delay

        return () => clearTimeout(timer);
    }, [navigate]);

    useEffect(() => {
        const interval = setInterval(() => {
            setDots((prev) => {
                if (prev === "...") {
                    return "";
                } else {
                    return prev + ".";
                }
            });
        }, 500);
        return () => clearTimeout(interval);
    }, [])

    return (
        <div id="main" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh' }}>
            <h1>Redirecting{dots}</h1>
            <CircularProgress />
        </div>
    );
};