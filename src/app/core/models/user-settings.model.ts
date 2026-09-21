export type ThemePreference = 'system' | 'light' | 'dark';

export interface UserSettings {
  showFavoriteVerseInNotes: boolean;
  theme: ThemePreference;
}
