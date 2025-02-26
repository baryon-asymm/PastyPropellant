# Исходные данные для расчета пористости каркасного слоя

# Массовые доли компонентов в топливном составе
mass_fractions = {
    'ХПЭПА': 0.21,
    'ПХА': 0.3,
    'HMX': 0.2827,
    'Al': 0.2073
}

# Плотности компонентов (в кг/м³)
densities = {
    'ХПЭПА': 950,
    'ПХА': 1952,
    'HMX': 1870,
    'Al': 2700
}

# Итоговая плотность топливного состава (в кг/м³)
overall_density = 1659

# Соотношения фракций ПХА для каждого состава
fractions = {
    'Bas_1': {'мелкая': 0.35, 'крупная': 0.65},
    'Bas_2': {'мелкая': 0.35, 'крупная': 0.65},
    'Bas_3': {'мелкая': 1.0, 'крупная': 0.0},
    'Bas_4': {'мелкая': 0.0, 'крупная': 1.0}
}

# Массовые доли компонентов в каркасном слое для каждого состава
core_mass_fractions = {
    'Bas_1': {'ХПЭПА': 0.4021, 'ПХА': 0.201, 'Al': 0.3969},
    'Bas_2': {'ХПЭПА': 0.4021, 'ПХА': 0.201, 'Al': 0.3969},
    'Bas_3': {'ХПЭПА': 0.4021, 'ПХА': 0.201, 'Al': 0.3968},
    'Bas_4': {'ХПЭПА': 0.5032, 'ПХА': 0.0,   'Al': 0.4968}
}

# Условные химические формулы для каждого состава
chemical_formulas = {
    'Bas_1': {
        'C': 9.590085,
        'H': 35.46467,
        'O': 15.78187,
        'N': 6.045435,
        'Cl': 3.343323,
        'Al': 14.71006
    },
    'Bas_2': {
        'C': 9.590085,
        'H': 35.46467,
        'O': 15.78187,
        'N': 6.045435,
        'Cl': 3.343323,
        'Al': 14.71006
    },
    'Bas_3': {
        'C': 9.590085,
        'H': 35.46467,
        'O': 15.78187,
        'N': 6.045435,
        'Cl': 3.343323,
        'Al': 14.71006
    },
    'Bas_4': {
        'C': 12.00132,
        'H': 35.81778,
        'O': 11.18614,
        'N': 5.424496,
        'Cl': 2.042992,
        'Al': 18.41259
    }
}

# Плотность углерода (в кг/м³)
density_carbon = 2267

print("Начало расчёта пористости каркасного слоя для топливных составов.")

def calculate_core_density(core_fractions, densities):
    # Расчёт объёмов каждого компонента в каркасе
    volume = sum(mass / density for mass, density in zip(core_fractions.values(), [densities['ХПЭПА'], densities['ПХА'], densities['Al']]))
    print(f"\tОбщий объём каркаса: {volume:.6f} м³")
    # Расчёт плотности каркаса
    return 1 / volume if volume != 0 else 0

def calculate_masses(chemical_formula):
    # Молярные массы элементов
    molar_mass = {
        'C': 12.01,
        'H': 1.008,
        'O': 16.00,
        'N': 14.01,
        'Cl': 35.45,
        'Al': 26.98
    }
    
    # Расчёт общей массы вещества по условной химической формуле
    total_mass = 0
    for element, count in chemical_formula.items():
        if element in molar_mass:
            total_mass += count * molar_mass[element]
    return total_mass / 1000  # Перевод в кг

def calculate_poreosity(core_density, core_fractions, chemical_formula):
    # Расчёт масс углерода и алюминия
    mass_C = chemical_formula['C'] * 12.01 / 1000
    mass_Al = chemical_formula['Al'] * 26.98 / 1000
    
    print(f"\tМасса углерода: {mass_C:.4f} кг")
    print(f"\tМасса алюминия: {mass_Al:.4f} кг")
    
    # Расчёт объёмов углерода и алюминия
    volume_C = mass_C / density_carbon
    volume_Al = mass_Al / densities['Al']
    
    print(f"\tОбъём углерода: {volume_C:.8f} м³")
    print(f"\tОбъём алюминия: {volume_Al:.8f} м³")
    
    # Расчёт общего объёма каркаса
    total_volume = (core_fractions['ХПЭПА'] / densities['ХПЭПА']) + \
                   (core_fractions['ПХА'] / densities['ПХА']) + \
                   (core_fractions['Al'] / densities['Al'])
    
    print(f"\tОбщий объём каркаса: {total_volume:.6f} м³")
    
    # Расчёт пористости
    poreosity = 1 - (volume_C + volume_Al) / total_volume
    return poreosity

# Основной расчет для каждого состава
results = {}
for bas in ['Bas_1', 'Bas_2', 'Bas_3', 'Bas_4']:
    print(f"\nРассчитываем пористость для {bas}:")
    
    core_fractions = core_mass_fractions[bas]
    chemical_formula = chemical_formulas[bas]
    
    # Расчёт плотности каркаса
    core_density = calculate_core_density(core_fractions, densities)
    print(f"\tПлотность каркаса: {core_density:.1f} кг/м³")
    
    # Расчёт пористости
    poreosity = calculate_poreosity(core_density, core_fractions, chemical_formula)
    
    results[bas] = {
        'плотность_каркаса': core_density,
        'пористость': poreosity
    }

# Вывод результатов
print("\nИтоговые результаты:")
for bas, data in results.items():
    print(f"\t{bas}:")
    print(f"\t\tПлотность каркаса: {data['плотность_каркаса']:.1f} кг/м³")
    print(f"\t\tПористость: {data['пористость']:.4f}")

print("\nКонец расчётов.")
