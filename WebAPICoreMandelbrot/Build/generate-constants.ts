#!/usr/bin/env node

/**
 * Simple TypeScript script to generate constants from C# SharedConstants class
 */

import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'fs';
import { dirname } from 'path';

const SOURCE_FILE = 'Constants/SharedConstants.cs';
const OUTPUT_FILE = 'src/shared-constants.ts';

interface Constant {
    name: string;
    value: string;
}

function main(): void {
    console.log('🔧 Generating TypeScript constants...');
    
    try {
        if (!existsSync(SOURCE_FILE)) {
            throw new Error(`Source file not found: ${SOURCE_FILE}`);
        }

        const content = readFileSync(SOURCE_FILE, 'utf8');
        const constants = extractConstants(content);
        
        if (constants.length === 0) {
            console.warn('⚠️  No constants found with [ExportToTypeScript] attribute');
            return;
        }
        
        const tsContent = generateTypeScript(constants);
        
        const outputDir = dirname(OUTPUT_FILE);
        if (!existsSync(outputDir)) {
            mkdirSync(outputDir, { recursive: true });
        }
        
        writeFileSync(OUTPUT_FILE, tsContent, 'utf8');
        console.log(`✅ Generated ${constants.length} constants in ${OUTPUT_FILE}`);
        
    } catch (error) {
        console.error(`❌ Error: ${(error as Error).message}`);
        process.exit(1);
    }
}

function extractConstants(content: string): Constant[] {
    const constants: Constant[] = [];
    
    // Simple regex to find constants with [ExportToTypeScript] attribute
    const regex = /\[ExportToTypeScript[^\]]*\]\s*public\s+const\s+\w+\s+(\w+)\s*=\s*([^;]+);/g;
    
    let match;
    while ((match = regex.exec(content)) !== null) {
        const name = match[1];
        const value = match[2].trim();
        constants.push({ name, value });
    }
    
    return constants;
}

function generateTypeScript(constants: Constant[]): string {
    const timestamp = new Date().toISOString().replace('T', ' ').substring(0, 19);
    
    let content = `// AUTO-GENERATED - DO NOT EDIT
// Generated at: ${timestamp}

`;

    constants.forEach(constant => {
        content += `export const ${constant.name} = ${constant.value};\n`;
    });
    
    content += `\nexport const SHARED_CONSTANTS = {\n`;
    constants.forEach(constant => {
        content += `    ${constant.name},\n`;
    });
    content += `} as const;\n`;
    
    return content;
}

if (require.main === module) {
    main();
}