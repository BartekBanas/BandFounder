import {VerifyEmailForm} from "../components/login/verifyEmailForm";
import {muiDarkTheme} from "../styles/muiDarkTheme";
import {ThemeProvider} from "@mui/material";

export function VerifyEmailPage() {
    return (
        <ThemeProvider theme={muiDarkTheme}>
            <VerifyEmailForm/>
        </ThemeProvider>
    );
}
