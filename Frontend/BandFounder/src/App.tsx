import React from 'react';
import './App.css';
import {Routing} from "./pages/Routing";
import {BrowserRouter} from "react-router-dom";
import {MantineProvider} from "@mantine/core";
import {Notifications} from '@mantine/notifications';
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';

function App() {
    return (
        <MantineProvider>
            <BrowserRouter>
                <Notifications/>
                <Routing/>
            </BrowserRouter>
        </MantineProvider>
    );
}

export default App;
