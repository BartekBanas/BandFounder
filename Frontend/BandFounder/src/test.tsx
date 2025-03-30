import {useEffect, useState} from "react";

export const Test = () => {
    const [socket, setSocket] = useState<WebSocket | null>(null);

    useEffect(() => {
        const chatRoomId = '519d30ec-6f42-4adf-b3af-2fc4dac8ef51'; // Replace with your actual chat room ID
        const ws = new WebSocket(`wss://localhost:7095/api/chatrooms?chatRoomId=${chatRoomId}`);

        ws.onopen = () => {
            console.log('WebSocket is open now.');
        };

        ws.onmessage = (event) => {
            console.log('Received message:', event.data);
        };

        ws.onclose = () => {
            console.log('WebSocket is closed now.');
        };

        ws.onerror = (error) => {
            console.error('WebSocket error:', error);
        };

        setSocket(ws);

        return () => {
            if (ws.readyState === WebSocket.OPEN) {
                ws.close();
            }
        };
    }, []);

    const sendMessage = (message: string) => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send(message);
        } else {
            console.error('WebSocket is not open.');
        }
    };

    return (
        <div>
            <h1>Test WebSocket</h1>
            <button onClick={() => sendMessage('Hello, WebSocket!')}>Send Message</button>
        </div>
    );
};

export default Test;