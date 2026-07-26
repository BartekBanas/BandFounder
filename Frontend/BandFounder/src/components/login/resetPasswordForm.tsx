import React, {useEffect, useMemo, useState} from "react";
import {useNavigate, useSearchParams} from "react-router-dom";
import {completePasswordReset, getPasswordResetInfo, getPublicProfilePicture} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {
    Alert,
    Avatar,
    Box,
    Button,
    CircularProgress,
    Divider,
    IconButton,
    InputAdornment,
    LinearProgress,
    Skeleton,
    Stack,
    TextField,
    ThemeProvider,
    Tooltip,
    Typography
} from "@mui/material";
import VisibilityIcon from "@mui/icons-material/Visibility";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import MailOutlineIcon from "@mui/icons-material/MailOutline";
import ScheduleIcon from "@mui/icons-material/Schedule";
import CakeOutlinedIcon from "@mui/icons-material/CakeOutlined";
import {designTokens, muiDarkTheme} from "../../styles/muiDarkTheme";
import {PasswordResetInfo} from "../../types/Account";

interface PasswordRule {
    label: string;
    isMet: (password: string) => boolean;
}

const passwordRules: PasswordRule[] = [
    {label: 'At least 8 characters', isMet: password => password.length >= 8},
    {label: 'A lowercase and an uppercase letter', isMet: password => /[a-z]/.test(password) && /[A-Z]/.test(password)},
    {label: 'A number', isMet: password => /\d/.test(password)},
    {label: 'A symbol', isMet: password => /[^A-Za-z0-9]/.test(password)},
];

const strengthLabels = ['Very weak', 'Weak', 'Fair', 'Good', 'Strong'];

function getStrengthColor(metCount: number): string {
    if (metCount <= 1) {
        return designTokens.errorMain;
    }

    if (metCount <= 2) {
        return '#f59e0b';
    }

    if (metCount === 3) {
        return designTokens.accentBlue;
    }

    return designTokens.successMain;
}

function formatRemaining(millisecondsLeft: number): string {
    const totalSeconds = Math.max(0, Math.floor(millisecondsLeft / 1000));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

export function ResetPasswordForm() {
    const [searchParams] = useSearchParams();
    const token = useMemo(() => searchParams.get('token') ?? '', [searchParams]);
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [info, setInfo] = useState<PasswordResetInfo | null>(null);
    const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
    const [loadingInfo, setLoadingInfo] = useState(token.length > 0);
    const [linkError, setLinkError] = useState<string | null>(token ? null : 'This reset link is invalid or incomplete.');
    const [submitting, setSubmitting] = useState(false);
    const [now, setNow] = useState(() => Date.now());
    const navigate = useNavigate();

    useEffect(() => {
        if (!token) {
            return;
        }

        let cancelled = false;

        (async () => {
            try {
                const resetInfo = await getPasswordResetInfo(token);
                if (cancelled) {
                    return;
                }

                setInfo(resetInfo);

                if (resetInfo.hasProfilePicture) {
                    const url = await getPublicProfilePicture(resetInfo.accountId);
                    if (!cancelled) {
                        setAvatarUrl(url);
                    }
                }
            } catch (error: any) {
                if (!cancelled) {
                    setLinkError(error?.message || 'This reset link is invalid or has expired.');
                }
            } finally {
                if (!cancelled) {
                    setLoadingInfo(false);
                }
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [token]);

    useEffect(() => {
        if (!info) {
            return;
        }

        const interval = setInterval(() => setNow(Date.now()), 1000);
        return () => clearInterval(interval);
    }, [info]);

    const metRules = passwordRules.filter(rule => rule.isMet(password)).length;
    const passwordsMatch = password.length > 0 && password === confirmPassword;
    const expiresAt = info ? new Date(info.expiresAt).getTime() : null;
    const millisecondsLeft = expiresAt === null ? null : expiresAt - now;
    const hasExpired = millisecondsLeft !== null && millisecondsLeft <= 0;
    const canSubmit = Boolean(info) && !hasExpired && !submitting && password.length > 0 && passwordsMatch;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!token || !info) {
            mantineErrorNotification('Reset token is missing');
            return;
        }

        if (password !== confirmPassword) {
            mantineErrorNotification('Passwords do not match');
            return;
        }

        setSubmitting(true);

        try {
            await completePasswordReset(token, password);
            mantineSuccessNotification('Password reset successfully');
            navigate('/');
        } catch (error: any) {
            mantineErrorNotification(error?.message || 'Failed to reset password');
            console.error(error);
        } finally {
            setSubmitting(false);
        }
    };

    const passwordVisibilitySlotProps = {
        input: {
            endAdornment: (
                <InputAdornment position="end">
                    <Tooltip title={showPassword ? 'Hide password' : 'Show password'}>
                        <IconButton
                            onClick={() => setShowPassword(previous => !previous)}
                            edge="end"
                            aria-label={showPassword ? 'Hide password' : 'Show password'}
                        >
                            {showPassword ? <VisibilityOffIcon/> : <VisibilityIcon/>}
                        </IconButton>
                    </Tooltip>
                </InputAdornment>
            ),
        },
    };

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Box
                sx={{
                    minHeight: '100vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    padding: '24px',
                    background: `linear-gradient(135deg, ${designTokens.authGradientStart}, ${designTokens.authGradientEnd})`,
                }}
            >
                <Box
                    component="form"
                    onSubmit={handleSubmit}
                    sx={{
                        width: '100%',
                        maxWidth: '480px',
                        padding: {xs: '28px', sm: '40px'},
                        borderRadius: '16px',
                        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.7)',
                        backgroundColor: 'background.paper',
                        color: 'text.primary',
                    }}
                >
                    <Typography variant="h4" align="center" fontWeight={600}>
                        Reset password
                    </Typography>
                    <Typography variant="body2" align="center" color="text.secondary" sx={{marginTop: 0.5}}>
                        Choose a new password for the account below.
                    </Typography>

                    <Box
                        sx={{
                            marginTop: 3,
                            padding: 2,
                            borderRadius: '12px',
                            backgroundColor: designTokens.cardSurfaceElevated,
                            boxShadow: `inset 0 0 0 1px ${designTokens.borderSubtle}`,
                        }}
                    >
                        {loadingInfo && (
                            <Stack direction="row" spacing={2} alignItems="center">
                                <Skeleton variant="circular" width={56} height={56}/>
                                <Box sx={{flexGrow: 1}}>
                                    <Skeleton variant="text" width="55%" height={26}/>
                                    <Skeleton variant="text" width="75%"/>
                                </Box>
                            </Stack>
                        )}

                        {!loadingInfo && info && (
                            <>
                                <Stack direction="row" spacing={2} alignItems="center">
                                    <Avatar
                                        src={avatarUrl ?? undefined}
                                        alt={info.username}
                                        sx={{
                                            width: 56,
                                            height: 56,
                                            fontSize: '1.35rem',
                                            fontWeight: 600,
                                            backgroundColor: designTokens.accentBlue,
                                            color: designTokens.textOnDark,
                                        }}
                                    >
                                        {info.username.charAt(0).toUpperCase()}
                                    </Avatar>
                                    <Box sx={{minWidth: 0}}>
                                        <Typography variant="h6" noWrap fontWeight={600}>
                                            {info.username}
                                        </Typography>
                                        <Stack direction="row" spacing={0.75} alignItems="center" sx={{minWidth: 0}}>
                                            <MailOutlineIcon
                                                sx={{fontSize: 16, color: designTokens.textMuted, flexShrink: 0}}/>
                                            <Typography variant="body2" color="text.secondary" noWrap>
                                                {info.email}
                                            </Typography>
                                        </Stack>
                                    </Box>
                                </Stack>

                                <Divider sx={{marginY: 1.5, borderColor: designTokens.borderSubtle}}/>

                                <Stack direction={{xs: 'column', sm: 'row'}} spacing={{xs: 0.5, sm: 2}}>
                                    <Stack direction="row" spacing={0.75} alignItems="center">
                                        <CakeOutlinedIcon sx={{fontSize: 16, color: designTokens.textMuted}}/>
                                        <Typography variant="caption" color="text.secondary">
                                            Member since {new Date(info.memberSince).toLocaleDateString()}
                                        </Typography>
                                    </Stack>
                                    <Stack direction="row" spacing={0.75} alignItems="center">
                                        <ScheduleIcon sx={{fontSize: 16, color: designTokens.textMuted}}/>
                                        <Typography
                                            variant="caption"
                                            sx={{color: hasExpired ? designTokens.errorMain : designTokens.textMuted}}
                                        >
                                            {hasExpired
                                                ? 'Link expired'
                                                : `Link expires in ${formatRemaining(millisecondsLeft ?? 0)}`}
                                        </Typography>
                                    </Stack>
                                </Stack>
                            </>
                        )}

                        {!loadingInfo && !info && (
                            <Alert
                                severity="error"
                                variant="outlined"
                                sx={{border: 'none', backgroundColor: 'transparent', padding: 0}}
                            >
                                {linkError}
                            </Alert>
                        )}
                    </Box>

                    {hasExpired && (
                        <Alert severity="warning" sx={{marginTop: 2}}>
                            This link has expired. Request a new one to continue.
                        </Alert>
                    )}

                    <TextField
                        fullWidth
                        label="New password"
                        type={showPassword ? 'text' : 'password'}
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        margin="normal"
                        disabled={!info || hasExpired}
                        autoComplete="new-password"
                        slotProps={passwordVisibilitySlotProps}
                    />

                    <TextField
                        fullWidth
                        label="Confirm password"
                        type={showPassword ? 'text' : 'password'}
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                        margin="normal"
                        disabled={!info || hasExpired}
                        autoComplete="new-password"
                        error={confirmPassword.length > 0 && !passwordsMatch}
                        helperText={
                            confirmPassword.length === 0
                                ? ' '
                                : passwordsMatch ? 'Passwords match' : 'Passwords do not match'
                        }
                        slotProps={passwordVisibilitySlotProps}
                    />

                    {password.length > 0 && (
                        <Box sx={{marginTop: 1}}>
                            <Stack direction="row" justifyContent="space-between" sx={{marginBottom: 0.5}}>
                                <Typography variant="caption" color="text.secondary">
                                    Password strength
                                </Typography>
                                <Typography variant="caption" sx={{color: getStrengthColor(metRules)}}>
                                    {strengthLabels[metRules]}
                                </Typography>
                            </Stack>
                            <LinearProgress
                                variant="determinate"
                                value={(metRules / passwordRules.length) * 100}
                                sx={{
                                    height: 6,
                                    borderRadius: 3,
                                    backgroundColor: designTokens.borderSubtle,
                                    '& .MuiLinearProgress-bar': {
                                        backgroundColor: getStrengthColor(metRules),
                                    },
                                }}
                            />
                            <Stack sx={{marginTop: 1}} spacing={0.25}>
                                {passwordRules.map(rule => {
                                    const met = rule.isMet(password);

                                    return (
                                        <Stack key={rule.label} direction="row" spacing={0.75} alignItems="center">
                                            {met
                                                ? <CheckCircleIcon
                                                    sx={{fontSize: 15, color: designTokens.successMain}}/>
                                                : <RadioButtonUncheckedIcon
                                                    sx={{fontSize: 15, color: designTokens.textMuted}}/>}
                                            <Typography
                                                variant="caption"
                                                sx={{color: met ? designTokens.textPrimary : designTokens.textMuted}}
                                            >
                                                {rule.label}
                                            </Typography>
                                        </Stack>
                                    );
                                })}
                            </Stack>
                        </Box>
                    )}

                    <Stack spacing={1.5} sx={{marginTop: 2}}>
                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            disabled={!canSubmit}
                            startIcon={submitting ? <CircularProgress size={16} color="inherit"/> : undefined}
                        >
                            {submitting ? 'Resetting…' : 'Reset password'}
                        </Button>
                        <Stack direction="row" spacing={1}>
                            <Button
                                onClick={() => navigate('/forgot-password')}
                                variant="outlined"
                                color="primary"
                                fullWidth
                            >
                                Request new link
                            </Button>
                            <Button
                                onClick={() => navigate('/')}
                                variant="outlined"
                                color="info"
                                fullWidth
                            >
                                Back to login
                            </Button>
                        </Stack>
                    </Stack>
                </Box>
            </Box>
        </ThemeProvider>
    );
}
