import {useEffect, useMemo, useState} from "react";
import {useNavigate, useSearchParams} from "react-router-dom";
import {confirmEmailVerification, resendEmailVerification} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Alert, Box, Button, CircularProgress, Stack, ThemeProvider, Typography} from "@mui/material";
import {designTokens, muiDarkTheme} from "../../styles/muiDarkTheme";
import {useIsAuthenticated} from "../../hooks/authentication";

export function VerifyEmailForm() {
    const [searchParams] = useSearchParams();
    const token = useMemo(() => searchParams.get("token") ?? "", [searchParams]);
    const [status, setStatus] = useState<"loading" | "success" | "error">(token ? "loading" : "error");
    const [message, setMessage] = useState(
        token ? "Verifying your email…" : "This verification link is missing a token.");
    const [resending, setResending] = useState(false);
    const isAuthenticated = useIsAuthenticated();
    const navigate = useNavigate();

    useEffect(() => {
        if (!token) {
            return;
        }

        let cancelled = false;

        (async () => {
            try {
                await confirmEmailVerification(token);
                if (!cancelled) {
                    setStatus("success");
                    setMessage("Your email is verified. You can close this page.");
                }
            } catch (error: any) {
                if (!cancelled) {
                    setStatus("error");
                    setMessage(error?.message || "This verification link is invalid or has expired.");
                }
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [token]);

    const handleResend = async () => {
        if (!isAuthenticated) {
            navigate("/");
            return;
        }

        setResending(true);
        try {
            await resendEmailVerification();
            mantineSuccessNotification("Verification email sent");
        } catch (error: any) {
            mantineErrorNotification(error?.message || "Failed to resend verification email");
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
                                {message}
                            </Typography>
                        </Stack>
                    )}

                    {status === "success" && (
                        <Alert severity="success" sx={{marginTop: 3}}>
                            {message}
                        </Alert>
                    )}

                    {status === "error" && (
                        <Alert severity="error" sx={{marginTop: 3}}>
                            {message}
                        </Alert>
                    )}

                    <Stack spacing={1.5} sx={{marginTop: 3}}>
                        {status !== "success" && (
                            <Button
                                variant="contained"
                                color="primary"
                                fullWidth
                                disabled={resending}
                                onClick={handleResend}
                            >
                                {isAuthenticated ? "Resend verification email" : "Log in to resend"}
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
