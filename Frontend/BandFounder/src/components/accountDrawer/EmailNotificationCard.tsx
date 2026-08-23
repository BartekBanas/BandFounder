import React, {FC} from 'react';
import {Switch} from '@mui/material';
import {
    EMAIL_UNREAD_DELAY_OPTIONS,
    resolveEmailUnreadDelayMinutes,
} from '../../constants/emailNotifications';

interface EmailNotificationCardProps {
    enabled: boolean;
    delayMinutes: number;
    disabled: boolean;
    onEnabledChange: (enabled: boolean) => void;
    onDelayChange: (minutes: number) => void;
}

export const EmailNotificationCard: FC<EmailNotificationCardProps> = ({
    enabled,
    delayMinutes,
    disabled,
    onEnabledChange,
    onDelayChange,
}) => {
    const selectedDelay = resolveEmailUnreadDelayMinutes(delayMinutes);

    return (
        <div className="utility-drawer__notification-card">
            <div className="utility-drawer__notification-row">
                <p className="utility-drawer__notification-title">Unread messages</p>
                <Switch
                    className="utility-drawer__notification-switch"
                    size="small"
                    checked={enabled}
                    disabled={disabled}
                    onChange={(_, checked) => onEnabledChange(checked)}
                    inputProps={{'aria-label': 'Email me about unread messages'}}
                />
            </div>

            <div
                className={`utility-drawer__delay-wrap${enabled ? ' is-open' : ''}`}
                aria-hidden={!enabled}
            >
                <div className="utility-drawer__delay-inner">
                    <div className="utility-drawer__delay-divider"/>
                    <span className="utility-drawer__delay-label" id="email-notification-delay-label">
                        Notify after
                    </span>
                    <div
                        className="utility-drawer__delay-pills"
                        role="radiogroup"
                        aria-labelledby="email-notification-delay-label"
                    >
                        {EMAIL_UNREAD_DELAY_OPTIONS.map((option) => {
                            const selected = option.minutes === selectedDelay;

                            return (
                                <button
                                    key={option.minutes}
                                    type="button"
                                    role="radio"
                                    aria-checked={selected}
                                    className={`utility-drawer__delay-pill${selected ? ' is-selected' : ''}`}
                                    disabled={disabled || !enabled}
                                    onClick={() => onDelayChange(option.minutes)}
                                >
                                    {option.label}
                                </button>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
};
