import {useCallback, useEffect, useRef, useState} from "react";
import {Box, Button, Modal, Stack, Typography} from "@mui/material";
import {
    EmailVerificationResendError,
    getMyAccount,
    resendEmailVerification
} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {muiDarkTheme} from "../../styles/muiDarkTheme";

const DISMISSED_KEY_PREFIX = "emailVerificationDismissed";

function getDismissedKey(accountId: string, email: string) {
    const normalizedEmail = email.trim().toLowerCase();
    return `${DISMISSED_KEY_PREFIX}:${encodeURIComponent(accountId)}:${encodeURIComponent(normalizedEmail)}`;
}

export function EmailVerificationPrompt() {
    const [open, setOpen] = useState(false);
    const [sending, setSending] = useState(false);
    const [resendAvailableAt, setResendAvailableAt] = useState<string | null>(null);
    const [now, setNow] = useState(() => Date.now());
    const verificationIdentityRef = useRef<string | null>(null);
    const accountRequestRef = useRef(0);

    const refreshAccount = useCallback(async () => {
        const requestId = ++accountRequestRef.current;

        try {
            const account = await getMyAccount();
            if (requestId !== accountRequestRef.current) {
                return;
            }

            const verificationIdentity = getDismissedKey(account.id, account.email);
            verificationIdentityRef.current = verificationIdentity;
            setSending(false);
            setResendAvailableAt(account.resendAvailableAt ?? null);
            setNow(Date.now());
            setOpen(
                account.emailVerified === false
                && sessionStorage.getItem(verificationIdentity) !== "true"
            );
        } catch {
            if (requestId === accountRequestRef.current) {
                verificationIdentityRef.current = null;
                setSending(false);
                setResendAvailableAt(null);
                setOpen(false);
            }
        }
    }, []);

    useEffect(() => {
        void refreshAccount();

        const refreshWhenVisible = () => {
            if (document.visibilityState === "visible") {
                void refreshAccount();
            }
        };

        window.addEventListener("focus", refreshAccount);
        document.addEventListener("visibilitychange", refreshWhenVisible);
        return () => {
            accountRequestRef.current++;
            window.removeEventListener("focus", refreshAccount);
            document.removeEventListener("visibilitychange", refreshWhenVisible);
        };
    }, [refreshAccount]);

    useEffect(() => {
        if (!open || !resendAvailableAt || Date.parse(resendAvailableAt) <= now) {
            return;
        }

        const interval = window.setInterval(() => setNow(Date.now()), 1000);
        return () => window.clearInterval(interval);
    }, [now, open, resendAvailableAt]);

    const dismiss = () => {
        if (verificationIdentityRef.current) {
            sessionStorage.setItem(verificationIdentityRef.current, "true");
        }
        setOpen(false);
    };

    const handleResend = async () => {
        const verificationIdentity = verificationIdentityRef.current;
        setSending(true);
        try {
            const result = await resendEmailVerification();
            if (verificationIdentityRef.current !== verificationIdentity) {
                return;
            }
            setResendAvailableAt(result.resendAvailableAt);
            setNow(Date.now());
            mantineSuccessNotification("Verification email queued for delivery");
        } catch (error: unknown) {
            if (verificationIdentityRef.current !== verificationIdentity) {
                return;
            }
            if (error instanceof EmailVerificationResendError) {
                setResendAvailableAt(error.resendAvailableAt);
                setNow(Date.now());
            } else {
                mantineErrorNotification(
                    error instanceof Error ? error.message : "Failed to resend verification email"
                );
            }
        } finally {
            if (verificationIdentityRef.current === verificationIdentity) {
                setSending(false);
            }
        }
    };

    const availableAtMilliseconds = resendAvailableAt ? Date.parse(resendAvailableAt) : 0;
    const secondsRemaining = Number.isNaN(availableAtMilliseconds)
        ? 0
        : Math.max(0, Math.ceil((availableAtMilliseconds - now) / 1000));
    const canResend = secondsRemaining === 0;

    return (
        <Modal open={open} onClose={dismiss}>
            <Box
                sx={{
                    position: "absolute",
                    top: "50%",
                    left: "50%",
                    transform: "translate(-50%, -50%)",
                    width: "auto",
                    maxWidth: 420,
                    bgcolor: muiDarkTheme.palette.background.default,
                    borderRadius: 2,
                    boxShadow: 24,
                    p: 4,
                }}
            >
                <Typography variant="h6" align="center">
                    Verify your email
                </Typography>
                <Typography variant="body2" align="center" sx={{color: "text.secondary", mt: 1.5}}>
                    We sent a verification email when you registered. Confirm your address so you
                    can receive message notifications. If delivery is delayed, it will retry
                    automatically.
                </Typography>
                {!canResend && (
                    <Typography variant="body2" align="center" sx={{mt: 2}}>
                        You can resend it in {secondsRemaining} seconds.
                    </Typography>
                )}
                <Stack direction="row" spacing={2} justifyContent="center" sx={{mt: 3}}>
                    <Button variant="outlined" onClick={dismiss}>
                        Close
                    </Button>
                    {canResend && (
                        <Button variant="contained" onClick={handleResend} disabled={sending}>
                            Resend email
                        </Button>
                    )}
                </Stack>
            </Box>
        </Modal>
    );
}
