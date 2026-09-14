import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImportScheduleComponent } from './import-schedule.component';

describe('ImportScheduleComponent', () => {
  let component: ImportScheduleComponent;
  let fixture: ComponentFixture<ImportScheduleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImportScheduleComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ImportScheduleComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
