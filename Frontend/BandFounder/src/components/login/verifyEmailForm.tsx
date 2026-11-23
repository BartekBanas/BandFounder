import {useState} from "react";
import {useNavigate, useSearchParams} from "react-router-dom";
import {confirmEmailVerification, resendEmailVerification} from "../../api/account";
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification,
} from "../common/mantineNotification";
import {Alert, Box, Button, CircularProgress, Stack, ThemeProvider, Typography} from "@mui/material";
import {designTokens, muiDarkTheme} from "../../styles/muiDarkTheme";
import {useIsAuthenticated} from "../../hooks/authentication";

export function VerifyEmailForm() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get("token") ?? "";
    return <EmailVerificationForm key={token} token={token}/>;
}

type Confirmation =
    | {status: "ready" | "loading" | "success"}
    | {status: "error"; message: string};

function EmailVerificationForm({token}: {token: string}) {
    const [confirmation, setConfirmation] = useState<Confirmation>({status: "ready"});
    const {status} = confirmation;
    const [resending, setResending] = useState(false);
    const isAuthenticated = useIsAuthenticated();
    const navigate = useNavigate();

    const handleConfirm = async () => {
        setConfirmation({status: "loading"});
        try {
            await confirmEmailVerification(token);
            setConfirmation({status: "success"});
        } catch (error) {
            setConfirmation({
                status: "error",
                message: error instanceof Error ? error.message : "Email verification failed. Try again.",
            });
        }
    };

    const handleResend = async () => {
        if (!isAuthenticated) {
            navigate("/");
            return;
        }

        setResending(true);
        try {
            const result = await resendEmailVerification();
            if (result.resendAvailableAt) {
                mantineSuccessNotification("Verification email queued for delivery");
            } else {
                mantineInformationNotification("Your signed-in account already has a verified email.");
            }
        } catch (error: unknown) {
            mantineErrorNotification(error instanceof Error ? error.message : "Failed to send verification email");
        } finally {
            setResending(false);
        }
    };

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Box
                sx={{
                    minHeight: "100vh",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    padding: "24px",
                    background: `linear-gradient(135deg, ${designTokens.authGradientStart}, ${designTokens.authGradientEnd})`,
                }}
            >
                <Box
                    sx={{
                        width: "100%",
                        maxWidth: "480px",
                        padding: {xs: "28px", sm: "40px"},
                        borderRadius: "16px",
                        boxShadow: "0 4px 20px rgba(0, 0, 0, 0.7)",
                        backgroundColor: "background.paper",
                        color: "text.primary",
                    }}
                >
                    <Typography variant="h4" align="center" fontWeight={600}>
                        Verify email
                    </Typography>

                    {status === "loading" && (
                        <Stack alignItems="center" spacing={2} sx={{marginTop: 4}}>
                            <CircularProgress size={28}/>
                            <Typography variant="body2" color="text.secondary">
                                Verifying your email…
                            </Typography>
                        </Stack>
                    )}

                    {status === "success" && (
                        <Alert severity="success" sx={{marginTop: 3}}>
                            Your email is verified.
                        </Alert>
                    )}

                    {status === "error" && (
                        <Alert severity="error" sx={{marginTop: 3}}>
                            {confirmation.message}
                        </Alert>
                    )}

                    <Stack spacing={1.5} sx={{marginTop: 3}}>
                        {token && (status === "ready" || status === "error") && (
                            <Button variant="contained" fullWidth onClick={handleConfirm}>
                                Confirm email
                            </Button>
                        )}
                        {(!token || status === "error") && (
                            <Button
                                variant="contained"
                                color="primary"
                                fullWidth
                                disabled={resending}
                                onClick={handleResend}
                            >
                                {isAuthenticated ? "Send verification email" : "Log in to request a link"}
                            </Button>
                        )}
                        <Button
                            variant="outlined"
                            color="info"
                            fullWidth
                            onClick={() => navigate(isAuthenticated ? "/home" : "/")}
                        >
                            {isAuthenticated ? "Back to home" : "Back to login"}
                        </Button>
                    </Stack>
                </Box>
            </Box>
        </ThemeProvider>
    );
}
