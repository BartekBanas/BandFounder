export const DEFAULT_EMAIL_UNREAD_DELAY_MINUTES = 1440;

export const EMAIL_UNREAD_DELAY_OPTIONS = [
    {minutes: 5, label: '5 min'},
    {minutes: 60, label: '1 hour'},
    {minutes: 1440, label: '1 day'},
] as const;

export function resolveEmailUnreadDelayMinutes(minutes: unknown): number {
    const value = Number(minutes);

    return EMAIL_UNREAD_DELAY_OPTIONS.some((option) => option.minutes === value)
        ? value
        : DEFAULT_EMAIL_UNREAD_DELAY_MINUTES;
}
