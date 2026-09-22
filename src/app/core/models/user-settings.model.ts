export type ThemePreference = 'system' | 'light' | 'dark';

export interface UserSettings {
  showFavoriteVerseInNotes: boolean;
  shareHistory: boolean;
  shareFavoriteVerseInHistory: boolean;
  enableAiCommentary: boolean;
  theme: ThemePreference;
}
