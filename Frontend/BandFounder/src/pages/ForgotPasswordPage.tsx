import {ForgotPasswordForm} from "../components/login/forgotPasswordForm";
import {muiDarkTheme} from "../styles/muiDarkTheme";
import {ThemeProvider} from "@mui/material";

export function ForgotPasswordPage() {
    return (
        <ThemeProvider theme={muiDarkTheme}>
            <ForgotPasswordForm/>
        </ThemeProvider>
    );
}
