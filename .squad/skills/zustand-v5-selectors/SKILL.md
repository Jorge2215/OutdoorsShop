# Zustand v5 Selectors

Zustand v5 uses Object.is for selector comparison. Object selectors (state) => ({...}) cause infinite re-renders (React error #185). Always use individual primitive selectors, e.g. (state) => state.foo and combine results where needed.
