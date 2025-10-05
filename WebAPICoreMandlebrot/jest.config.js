module.exports = {
    preset: 'ts-jest',
    testEnvironment: 'jsdom',
    roots: ['<rootDir>/src', '<rootDir>/tests'],
    testMatch: [
        '<rootDir>/tests/**/*.test.ts',
        '<rootDir>/tests/**/*.spec.ts'
    ],
    transform: {
        '^.+\\.ts$': 'ts-jest'
    },
    collectCoverageFrom: [
        'src/**/*.ts',
        '!src/shared-constants.ts', // Exclude generated file
        '!src/**/*.d.ts'
    ],
    coverageDirectory: 'coverage',
    coverageReporters: [
        'text',
        'text-summary',
        'html',
        'lcov',
        'cobertura'
    ],
    coverageThreshold: {
        global: {
            branches: 70,
            functions: 70,
            lines: 70,
            statements: 70
        }
    },
    setupFilesAfterEnv: ['<rootDir>/tests/setup.ts'],
    moduleNameMapper: {
        '^@/(.*)$': '<rootDir>/src/$1'
    },
    globals: {
        'ts-jest': {
            useESM: false
        }
    }
};