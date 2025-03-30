import {LoginForm} from "../components/login/loginForm";
import {muiDarkTheme} from "../styles/muiDarkTheme";
import {ThemeProvider} from "@mui/material";


export function LoginPage() {
    return (
        <div>
            <ThemeProvider theme={muiDarkTheme}>
                <LoginForm/>
            </ThemeProvider>
        </div>
    );
}