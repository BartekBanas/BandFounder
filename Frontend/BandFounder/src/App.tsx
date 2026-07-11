import {applyThemeVariables} from './styles/designTokens';
import './styles/theme.css';
import './App.css';

applyThemeVariables();
import {Routing} from "./pages/Routing";
import {BrowserRouter} from "react-router-dom";
import {MantineProvider} from "@mantine/core";
import {Notifications} from '@mantine/notifications';
import {ThemeProvider} from '@mui/material';
import {muiDarkTheme} from './styles/muiDarkTheme';
import {AeroBackdrop} from './components/common/AeroBackdrop';
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import '../src/pages/styles/cssReset.css';

function App() {
    return (
        <ThemeProvider theme={muiDarkTheme}>
            <MantineProvider>
                <AeroBackdrop/>
                <BrowserRouter>
                    <Notifications/>
                    <Routing/>
                </BrowserRouter>
            </MantineProvider>
        </ThemeProvider>
    );
}

export default App;
