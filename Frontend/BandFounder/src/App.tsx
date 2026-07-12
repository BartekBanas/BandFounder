import {applyThemeVariables} from './styles/designTokens';
import './styles/theme.css';
import './App.css';

applyThemeVariables();
import {Routing} from "./pages/Routing";
import {BrowserRouter} from "react-router-dom";
import {MantineProvider} from "@mantine/core";
import {mantineTheme} from './styles/mantineTheme';
import {Notifications} from '@mantine/notifications';
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import '../src/pages/styles/cssReset.css';

function App() {
    return (
        <MantineProvider theme={mantineTheme}>
            <BrowserRouter>
                <Notifications/>
                <Routing/>
            </BrowserRouter>
        </MantineProvider>
    );
}

export default App;
