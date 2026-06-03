export type LogRecord = {
    id: number;
    sessionId?: string;
    timestamp: string;
    severityText: string;
    severityNumber: number;
    body: string;
    bodySegments?: MessageSegment[];
    source?: string;
    category?: string;
    loggerName?: string;
    attributes?: Record<string, unknown>;
    resource?: Record<string, unknown>;
    scope?: Record<string, unknown>;
};

export type Status = {
    enabled: boolean;
    sessionId?: string;
    sessionStartedAtUtc?: string;
    processId?: number;
    url?: string;
    accessMode?: "loopback" | "lan";
    lanAccessEnabled?: boolean;
    lanUrls?: string[];
    bufferCount: number;
    bufferCapacity: number;
    queueDepth: number;
    queueCapacity: number;
    dropped: number;
};

export type MessageSegment = {
    text: string;
    color?: string;
    bold?: boolean;
    dim?: boolean;
    kind?: string;
};

export type ThemeMode = "dark" | "light";

export type ConnectionState = "connecting" | "connected" | "reconnecting";

export type NoiseRule = {
    pattern: string;
    enabled: boolean;
    tipKey?: string;
};
