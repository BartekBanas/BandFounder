import {ResetPasswordForm} from "../components/login/resetPasswordForm";
import {muiDarkTheme} from "../styles/muiDarkTheme";
import {ThemeProvider} from "@mui/material";

export function ResetPasswordPage() {
    return (
        <ThemeProvider theme={muiDarkTheme}>
            <ResetPasswordForm/>
        </ThemeProvider>
    );
}
