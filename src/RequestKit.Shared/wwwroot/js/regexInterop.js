export function execute(pattern, testString, flags) {
    try {
        const regex = new RegExp(pattern, flags);
        const matches = [];

        if (flags.includes("g")) {
            let match;
            while ((match = regex.exec(testString)) !== null) {
                const groups = [];
                for (let i = 1; i < match.length; i++) {
                    groups.push({
                        groupIndex: i,
                        name: null,
                        value: match[i] ?? "",
                        index: match.index,
                        length: (match[i] ?? "").length
                    });
                }
                // Named groups
                if (match.groups) {
                    for (const [name, value] of Object.entries(match.groups)) {
                        const existing = groups.find(g => g.value === (value ?? "") && !g.name);
                        if (existing) existing.name = name;
                    }
                }
                matches.push({
                    index: match.index,
                    length: match[0].length,
                    value: match[0],
                    groups
                });
                if (!match[0].length) regex.lastIndex++;
            }
        } else {
            const match = regex.exec(testString);
            if (match) {
                const groups = [];
                for (let i = 1; i < match.length; i++) {
                    groups.push({
                        groupIndex: i,
                        name: null,
                        value: match[i] ?? "",
                        index: match.index,
                        length: (match[i] ?? "").length
                    });
                }
                if (match.groups) {
                    for (const [name, value] of Object.entries(match.groups)) {
                        const existing = groups.find(g => g.value === (value ?? "") && !g.name);
                        if (existing) existing.name = name;
                    }
                }
                matches.push({
                    index: match.index,
                    length: match[0].length,
                    value: match[0],
                    groups
                });
            }
        }

        return { success: true, matches, errorMessage: null };
    } catch (e) {
        return { success: false, matches: [], errorMessage: e.message };
    }
}
