import React from 'react';
import './App.css';
import {Routing} from "./pages/Routing";
import {BrowserRouter} from "react-router-dom";
import {MantineProvider} from "@mantine/core";
import {Notifications} from '@mantine/notifications';
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import {useMuiNotification} from "./components/common/useMuiNotification";
import {NotificationProvider} from "./components/common/NotificationProvider";

function App() {
    const {
        NotificationComponent,
        showSuccessMuiNotification,
        showInfoMuiNotification,
        showErrorMuiNotification
    } = useMuiNotification();

    return (
        <MantineProvider>
            <BrowserRouter>
                <Notifications/>
                <NotificationProvider>
                    <Routing />
                </NotificationProvider>
            </BrowserRouter>
        </MantineProvider>
    );
}

export default App;
